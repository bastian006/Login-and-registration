using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Login_and_registration.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Login_and_registration.Controllers;

public class UserController : Controller
{
    private readonly ILogger<UserController> _logger;
    private MyContext _context;

    public UserController(ILogger<UserController> logger, MyContext context)
    {
        _logger  = logger;
        _context = context;
    }

    //--------------------------------------
    //--------------- ROUTES ---------------

    // Index - Displays the Registration and Login Forms
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    // CreateUser - Adds User to DB, Adds UserId to Session & Redirects to Success View
    [HttpPost("users/create")]
    public IActionResult CreateUser(User newUser)
    {
        // If Valid Registration: Hash password, add User to DB, and redirect to Success route
        if(ModelState.IsValid)
        {
            PasswordHasher<User> Hasher = new PasswordHasher<User>();
            newUser.Password = Hasher.HashPassword(newUser, newUser.Password);
            _context.Add(newUser);
            _context.SaveChanges();
            HttpContext.Session.SetInt32("UserId", newUser.UserId);
            return RedirectToAction("Success");
        }
        // If Invalid Registration: throw errors and return Index route
        else
        {
            return View("Index");
        }
    }

    // LoginUser - Adds UserId to Session & Redirects to Success View
    [HttpPost("users/login")]
    public IActionResult LoginUser(LoginUser loginUser)
    {
        if(ModelState.IsValid)
        {
            // If Email Not in DB: Throw Error & Return Index View
            User? userInDb = _context.Users.FirstOrDefault(u => u.Email == loginUser.LEmail);
            if(userInDb == null)
            {
                ModelState.AddModelError("LEmail", "Invalid Email/Password");
                return View("Index");
            }

            // If Email in DB: Hash Provided PW and Compare Against Hashed PW in DB
            PasswordHasher<LoginUser> hasher = new PasswordHasher<LoginUser>();
            var result = hasher.VerifyHashedPassword(loginUser, userInDb.Password, loginUser.LPassword);
            // If No Match: Throw Error and Return Index View
            if (result == 0)
            {
                ModelState.AddModelError("LEmail", "Invalid Email/Password");
                return View("Index");
            }
            // If Match: Add UserId to Session and Redirect to Success View
            else
            {
                HttpContext.Session.SetInt32("UserId", userInDb.UserId);
                return RedirectToAction("Success");
            }
        }
        // If Invalid Login: Throw Error and Return Index View
        else
        {
            return View("Index");
        }
    }

    // LogoutUser - Clears Session & Redirects to Index View
    [HttpPost("users/logout")]
    public IActionResult LogoutUser()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }

    // Success - Displays the Success View
    [SessionCheck]
    [HttpGet("success")]
    public IActionResult Success()
    {
        return View();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

//----------------------------------------------
//--------------- CUSTOM METHODS ---------------
public class SessionCheckAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        int? userId = context.HttpContext.Session.GetInt32("UserId");
        if(userId == null)
        {
            context.Result = new RedirectToActionResult("Index", "User", null);
        }
    }
}