using System.Net.Mail;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using HotelRoomReservationSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis.Scripting;
using BCrypt.Net;
using HotelRoomReservationSystem.Models;
using NuGet.Common;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using HotelRoomReservationSystem;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Http;
using DocumentFormat.OpenXml.Spreadsheet;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Twilio.Clients;
using HotelRoomReservationSystem.Services;
using Microsoft.Extensions.Options;
using PayPal.Api.OpenIdConnect;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using HotelRoomReservationSystem.BLL.Interfaces;

namespace HotelRoomReservationSystem.Controllers;

public class AccountController : Controller
{
    private static readonly object LockObject = new object();
    private readonly HotelRoomReservationDB db;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment en;
    private readonly ILogger<AccountController> _logger;
    private readonly Helper hp;
    private readonly ITwilioRestClient _client;
    private readonly IMembershipService membershipService;
    private readonly IMembershipRewardsService membershipRewardsService;
    private readonly IRewardsService rewardsService;

    public AccountController(ILogger<AccountController> logger,
        IConfiguration configuration, IWebHostEnvironment en, HotelRoomReservationDB db, Helper hp,
        ITwilioRestClient client, IMembershipService membershipService, IMembershipRewardsService membershipRewardsService
        , IRewardsService rewardsService)
    {
        this.db = db;
        _logger = logger;
        _configuration = configuration;
        this.hp = hp;
        this.en = en;
        _client = client;
        this.membershipService = membershipService ?? throw new ArgumentNullException(nameof(membershipService));
        this.membershipRewardsService = membershipRewardsService;
        this.rewardsService = rewardsService;
    }

    public IActionResult AccessDenied(string? returnURL)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        ViewBag.UserRole = userRole;
        return View();
    }

    public IActionResult Index()
    {
        return View();
    }


    //FORGET PASSWORD SECTION

    [AllowAnonymous]
    public IActionResult forgetPassword()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> forgetPassword(forgotPasswordVM model)
    {
        var u = db.Users.FirstOrDefault(user => user.Email == model.Email);
        if (u == null)
        {
            ModelState.AddModelError("", "This Email has not been registered.");
            return View(model);
        }

        if (ModelState.IsValid)
        {
            await HandleTokenGeneration(u.Id, "forgot");
            //HttpContext.Session.SetString("UserIdForResetPassword", u.Id);

        }
        else
        {
            return View(model);
        }
        return View("VerifyEmail", "Account");
    }

    [AllowAnonymous]
    public IActionResult resetPassword()
    {
        var userId = TempData["UserId"] as string;
        if (userId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }
        Console.WriteLine(userId);
        Console.WriteLine("Resetting password for UserId: " + userId);
        TempData["UserId"] = userId;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public IActionResult resetPassword(resetPasswordVM model)
    {
        if (!ModelState.IsValid)
        {
            return View(model); // Return the view with validation errors
        }
        else
        {
            var userId = TempData["UserId"] as string;
            Console.WriteLine("Resetting password for UserId: " + userId);
            if (userId == null)
            {
                ModelState.AddModelError("", "UserId is not set.");
                return View(model); // Return view if UserId is null
            }

            Console.WriteLine("Resetting password for UserId: " + userId); // Debugging output
            var user = db.Users.Find(userId); // Retrieve user from the database based on UserId

            if (user != null)
            {
                user.Password = hp.HashPassword(model.Password); // Hash the new password
                db.SaveChanges(); // Save the changes to the database

                TempData["SuccessMessage"] = "Password reset successfully!";
                return RedirectToAction("Login", "Account"); // Redirect to the home page
            }
            else
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }
        }
    }

    //FORGET PASSWORD SECTION


    //DISPLAY PROFILE
    [Authorize(Roles = "Customer")]
    public IActionResult Profile()
    {
        var profile = GetProfileVM();
        return View(profile);
    }

    //DISPLAY PROFILE

    //EDIT PROFILE 

    [Authorize(Roles = "Customer")]
    public IActionResult EditProfile(string? returnUrl)
    {

        var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
        var user = GetUserByEmail(userEmail);

        if (user == null)
        {
            return NotFound(); // Return a 404 if user is not found
        }

        var userModel = GetUsersModels(user.Id);

        if (userModel == null)
        {
            return NotFound(); // Return a 404 if the profile is not found
        }

        return View(userModel);
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> EditProfile(UpdateUserVM model)
    {
        var claimsIdentity = User.Identity as ClaimsIdentity;
        var existingUser = db.Users.FirstOrDefault(u => u.Id == model.id);

        if (existingUser == null)
        {
            ModelState.AddModelError("", "User not found.");
            return View(model);
        }

        if (!ValidateUserDetails(model, existingUser))
        {
            model.portrait = existingUser.Portrait;
            PrepareModelForReturn(model, existingUser);
            return View(model);
        }

        bool isUnchanged =
            existingUser.Name == $"{model.FirstName} {model.LastName}" &&
             existingUser.Email == model.Email &&
                existingUser.PhoneNum == model.PhoneNum &&
            existingUser.DOB == model.BirthDay &&
             string.IsNullOrEmpty(model.Base64Photo) &&
             model.Photo == null;

        if (isUnchanged)
        {
            TempData["infoMessage"] = "No changes detected in your profile.";
            return RedirectToAction("Profile");
        }

        if (ModelState.IsValid)
        {
            UpdateExistingUser(existingUser, model);
            UpdateUserClaims(existingUser, claimsIdentity);

            db.SaveChanges();

            TempData["SuccessMessage"] = "Profile updated successfully!";

            return RedirectToAction("Profile");
        }

        PrepareModelForReturn(model, existingUser);
        return View(model);
    }

    //GET PROFILE
    private profileVM GetProfileVM()
    {
        var claimsIdentity = User.Identity as ClaimsIdentity;
        if (claimsIdentity == null) return null;

        var email = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value;
        var name = claimsIdentity.FindFirst("Names")?.Value;

        var user = db.Users.FirstOrDefault(u => u.Email == email);
        if (user == null) return null;

        var nameParts = name?.Split(' ') ?? Array.Empty<string>();
        var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
        var lastName = string.Join(" ", nameParts.Skip(1));

        var membership = membershipService.GetMember(user.Id);
        if (membership != null)
        {
            var membershipRw = membershipRewardsService.GetAllByMM(membership.Id);
        }

        return new profileVM
        {
            Id = user.Id,
            FirstName = firstName,
            LastName = lastName,
            Email = user.Email,
            PhoneNum = user.PhoneNum,
            Portrait = user.Portrait,
            BirthDay = user.DOB,
            CreatedAt = user.DateCreated,
            Rewards = rewardsService.GetAllRewards()
        };
    }
    //EDIT PROFILE 


    //CHANGE PASSWORD SECTION IN THE PROFILE
    [Authorize(Roles = "Customer")]
    public IActionResult Security()
    {
        var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
        var user = GetUserByEmail(userEmail);
        var mfa = db.MFALogin.FirstOrDefault(m => m.UsersId == user.Id);
        ViewBag.MFAStatus = mfa.status;

        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public IActionResult Security(ResetPasswordVM model)
    {

        var claimsIdentity = User.Identity as ClaimsIdentity;
        var email = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value;
        var u = db.Users.FirstOrDefault(user => user.Email == email);

        if (model.currentPassword == null)
        {
            ModelState.AddModelError("currentPassword", "Current Password is Required.");
            return View(model);
        }

        if (!hp.VerifyPassword(model.currentPassword, u.Password))
        {
            ModelState.AddModelError("currentPassword", "Current Password Incorrect.");
            return View(model);
        }

        if (model.currentPassword.Equals(model.Password))
        {
            ModelState.AddModelError("Password", "Current and New Password are Same.");
            return View(model);
        }

        if (ModelState.IsValid)
        {
            u.Password = hp.HashPassword(model.Password);
            db.SaveChanges();
            TempData["SuccessMessage"] = "Password updated successfully!";

            return RedirectToAction("Profile");
        }
        else
        {
            //ModelState.AddModelError("", "Please Enter new Password accordingly.");
        }

        return View(model);
    }

    //CHANGE PASSWORD SECTION IN THE PROFILE


    //LOG IN OR OUT AND REGISTER SECTION

    [AllowAnonymous]
    public IActionResult RegisterForm()
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("AccessDenied", "Account");
        }
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterForm(RegistrationVM model)
    {
        bool hasErrors = false;

        hasErrors |= ValidateField(() => CheckEmailExist(model.Email), "Email", "This Email has been registered. Please try another.");
        hasErrors |= ValidateField(() => CheckPhoneExist(model.PhoneNum), "PhoneNum", "This Phone Number has been registered. Please try another.");

        if (hasErrors)
        {
            return View(model);
        }

        if (model.RecaptchaToken == null)
        {
            ModelState.AddModelError("RecaptchaToken", "Captcha is required.");
            return View(model);
        }

        if (ModelState.IsValid)
        {
            string userId = await GenerateUserIdAsync();
            var customer = new Customer
            {
                Id = userId,
                Name = model.FirstName + " " + model.LastName,
                Email = model.Email,
                Password = hp.HashPassword(model.Password),
                PhoneNum = model.PhoneNum,

                DateCreated = DateTime.Now,
                DOB = model.BirthDay,
                Portrait = "default_profile.jpg",
                Status = "PENDING"
                //Discriminator = "Customer",
            };

            db.Users.Add(customer);

            var mfa = new MFALogin
            {
                status = "OFF",
                UsersId = userId,
                otp = "000000",
                ExipredDateTime = new DateTime(1, 1, 1)
            };
            db.MFALogin.Add(mfa);
            db.SaveChanges();

            membershipService.AddNewMember(userId);
            await HandleTokenGeneration(userId, "REGISTER");
            //HttpContext.Session.SetString("UserIdForRegistration", userId);
        }
        else
        {
            return View(model);
        }

        return View("VerifyEmail", "Account");
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginVM model, string? returnURL)
    {
        var malaysiaTimeOffset = new TimeSpan(8, 0, 0);

        var u = db.Users.FirstOrDefault(user => user.Email == model.email);

        if (u == null)
        {
            ModelState.AddModelError("", "Invalid login attempt.");
            return View();
        }

        if (u.Status == "BLOCK")
        {
            ModelState.AddModelError("", "This Account has been block.");
            return View(model);
        }
        else if (u.Status == "DELETED")
        {
            ModelState.AddModelError("", "This Account has been Deleted. Please click 'Recover Account' button to recover your account.");
            return View(model);
        }
        else if (u.Status != "VERIFY")
        {
            ModelState.AddModelError("", "This Account has not been Verified.");
            return View(model);
        }
        var loginAttempt = db.LoginAttempt.FirstOrDefault(a => a.UsersId == u.Id);

        if (loginAttempt?.IsLocked == true)
        {
            if (loginAttempt.LockoutEndTime.HasValue && loginAttempt.LockoutEndTime.Value > DateTime.UtcNow.Add(malaysiaTimeOffset))
            {
                //var remainingTime = loginAttempt.LockoutEndTime - DateTime.UtcNow.Add(malaysiaTimeOffset);

                ViewBag.LockoutEndTime = loginAttempt.LockoutEndTime.Value.ToString("o");
                ModelState.AddModelError("", $"Account is locked. Try again after {loginAttempt.LockoutEndTime}.");
                return View(model);
            }
            else
            {
                loginAttempt.IsLocked = false;
                loginAttempt.FailedLoginAttempts = 0;
                db.SaveChanges();
            }
        }

        if (!hp.VerifyPassword(model.password, u.Password))
        {
            if (loginAttempt == null)
            {
                loginAttempt = new LoginAttempt
                {
                    UsersId = u.Id,
                    FailedLoginAttempts = 1,
                    IsLocked = false,
                    LockoutEndTime = null,
                };
                db.LoginAttempt.Add(loginAttempt);
            }
            else
            {
                loginAttempt.FailedLoginAttempts++;
                if (loginAttempt.FailedLoginAttempts >= 3)
                {
                    loginAttempt.IsLocked = true;
                    loginAttempt.LockoutEndTime = DateTime.UtcNow.AddMinutes(15).Add(malaysiaTimeOffset);
                }

            }


            db.SaveChanges();
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        if (loginAttempt != null)
        {
            loginAttempt.FailedLoginAttempts = 0;
            loginAttempt.IsLocked = false;
            db.SaveChanges();
        }

        if (ModelState.IsValid)
        {
            var mfa = db.MFALogin.FirstOrDefault(m => m.UsersId == u.Id);

            if (mfa == null)
            {

                ModelState.AddModelError("", "MFA information not found. Please Try Again later.");
                return View(model);
            }
            else
            {
                if (mfa.status == "ON" && u.Role == "Customer")
                {
                    HttpContext.Session.SetString("UserIdForMFA", mfa.UsersId);
                    HttpContext.Session.SetString("MyBoolKey", model.RememberMe.ToString());
                    //redirect to mfa page to perform second enter password
                    return RedirectToAction("mfaPage", "Account");
                }
                else
                {
                    hp.SignIn(u!.Email, u.Role, model.RememberMe, u.Name, u.Portrait);
                    var userJson = JsonSerializer.Serialize(u);
                    HttpContext.Session.SetString("User", userJson);

                    TempData["loginSuccess"] = $" ✅ Welcome {u.Name}, login successful.";
                    if (u.Role == "Admin" || u.Role == "Manager")
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
            }

        }
        else
        {
            return View(model);
        }

    }
    [Authorize]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        hp.SignOut();
        TempData["logoutMessage"] = "🔒 You have successfully logged out. See you next time!";
        return RedirectToAction("Index", "Home");
    }

    //LOG IN OR OUT AND REGISTER SECTION




    //Email Verification Purpose
    //RESET PASSWORD AND Account Activation

    [HttpGet("Account/VerifyEmail")]
    [AllowAnonymous]
    public IActionResult VerifyEmail()
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        ViewBag.Message = TempData["Message"]?.ToString();
        ViewBag.IsSuccess = TempData["IsSuccess"] as bool? ?? false;

        return View("VerifyEmail");
    }

    [HttpGet("Account/VerifyEmail/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("AccessDenied", "Account");
        }
        if (string.IsNullOrEmpty(token))
        {
            ViewBag.Message = "Invalid token.";
            ViewBag.IsSuccess = false;
            return View("VerifyEmail");
        }

        var tokenEntity = await db.Token
            .Where(t => t.token == token)
            .FirstOrDefaultAsync();

        if (tokenEntity == null)
        {
            ViewBag.Message = "Token not found.";
            ViewBag.IsSuccess = false;
            return View("VerifyEmail");
        }
        var user = await db.Users.FindAsync(tokenEntity.UsersId);

        if (user == null)
        {
            ViewBag.Message = "User not found.";
            ViewBag.IsSuccess = false;
            return View("VerifyEmail");
        }
        if (tokenEntity.Purpose == "COMPLETED")
        {
            ViewBag.UserId = user.Id;
            ViewBag.Message = "Your email has already been verified. No further action is required.";

            return View("VerifyEmail");
        }
        // Check if the token has expired
        if (DateTime.Now > tokenEntity.Expiration)
        {
            ViewBag.Message = "Token has expired.";
            ViewBag.UserId = user.Id;
            ViewBag.token = tokenEntity.token;
            ViewBag.IsSuccess = false;
            return View("VerifyEmail");
        }
        if (ViewBag.Purpose != null && ViewBag.Purpose != "")
        {
            Console.WriteLine(ViewBag.Purpose);
        }
        else
        {
            Console.WriteLine("ViewBag.Purpose is null or empty.");
        }
        //ViewBag.Purpose = tokenEntity.Purpose;
        if (tokenEntity.Purpose == "forgot" || tokenEntity.Purpose == "ResendResetPassword")
        {
            TempData["UserId"] = user.Id;
            Console.WriteLine("ViewBag.UserId set to: " + ViewBag.UserId);
            tokenEntity.Purpose = "COMPLETED";
            await db.SaveChangesAsync();
            return RedirectToAction("resetPassword");
        }
        else if (tokenEntity.Purpose == "REGISTER" || tokenEntity.Purpose == "ResendRegistration")
        {
            if (user != null)
            {
                // user.Roles = "Verified"; 
                user.Status = "VERIFY";
                db.Update(user);
                tokenEntity.Purpose = "COMPLETED";
                await db.SaveChangesAsync();

                ViewBag.UserId = user.Id;
                ViewBag.Message = "Email verification successful.";
                ViewBag.IsSuccess = true;

                return View("VerifyEmail");
            }
        }
        else if (tokenEntity.Purpose == "RECOVER")
        {
            user.Status = "VERIFY";
            db.Users.Update(user);
            tokenEntity.Purpose = "COMPLETED";
            await db.SaveChangesAsync();
            ViewBag.UserId = user.Id;
            ViewBag.Message = "Your account has been successfully recovered through email verification. If you forget your password, please use the \"Forgot Password\" function.";
            ViewBag.IsSuccess = true;
            return View("VerifyEmail");
        }



        ViewBag.Message = "User not found.";
        ViewBag.IsSuccess = false;
        return View("VerifyEmail");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification()
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("AccessDenied", "Account");
        }
        string tokenQuery = HttpContext.Request.Query["tempToken"];

        var tokenEntity = await db.Token
            .Where(t => t.token == tokenQuery)
            .FirstOrDefaultAsync();

        if (tokenEntity == null)
        {
            ViewBag.Message = "Token not found.";
            ViewBag.IsSuccess = false;
            return RedirectToAction("VerifyEmail");
        }
        var user = await db.Users.FindAsync(tokenEntity.UsersId);

        if (tokenEntity.Purpose == "ResendResetPassword" || tokenEntity.Purpose == "forgot")
        {
            await HandleTokenGeneration(tokenEntity.UsersId, "ResendResetPassword");
            ViewBag.UserId = null;
            return RedirectToAction("VerifyEmail");
        }
        else if (tokenEntity.Purpose == "ResendRegistration" || tokenEntity.Purpose == "REGISTER")
        {
            await HandleTokenGeneration(tokenEntity.UsersId, "ResendRegistration");
            ViewBag.UserId = null;
            return View("VerifyEmail");

        }
        return View("VerifyEmail", "Account");
    }

    //RESEND WHEN FAILE
    [HttpGet]
    [Route("Account/ResendForFailed")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerifications()
    {

        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("AccessDenied", "Account");
        }
        //string userIdFromQuery = HttpContext.Request.Query["userId"];
        string tokenFromQuery = HttpContext.Request.Query["Token"];

        Console.WriteLine("tokenFromQuery");
        Console.WriteLine(tokenFromQuery);
        var tokenEntity = await db.Token
            .Where(t => t.token == tokenFromQuery)
            .FirstOrDefaultAsync();

        Console.WriteLine("userIdFromQuery");
        //Console.WriteLine(userIdFromQuery);
        Console.WriteLine("tokenFromQuery");
        Console.WriteLine(tokenFromQuery);

        if (tokenEntity == null)
        {
            ViewBag.Message = "Token not found.";
            ViewBag.IsSuccess = false;
            return View("VerifyEmail");
        }
        var user = await db.Users.FindAsync(tokenEntity.UsersId);

        if (tokenEntity.Purpose == "ResendResetPassword" || tokenEntity.Purpose == "forgot")
        {
            await HandleTokenGeneration(tokenEntity.UsersId, "ResendResetPassword");
            ViewBag.UserId = null;
            return RedirectToAction("VerifyEmail");
        }
        else if (tokenEntity.Purpose == "ResendRegistration" || tokenEntity.Purpose == "REGISTER")
        {
            await HandleTokenGeneration(tokenEntity.UsersId, "ResendRegistration");
            ViewBag.UserId = null;
            return RedirectToAction("VerifyEmail");

        }
        else if (tokenEntity.Purpose == "RECOVER")
        {
            await HandleTokenGeneration(tokenEntity.UsersId, "RECOVER");
            ViewBag.UserId = null;
            return RedirectToAction("VerifyEmail");
            //user.Status = "VERIFY";
            //db.Users.Update(user);
            //await db.SaveChangesAsync();
            //ViewBag.UserId = user.Id;
            //ViewBag.Message = "Your account has been successfully recovered through email verification. If you forget your password, please use the \"Forgot Password\" function.";
            //ViewBag.IsSuccess = true;
            //return View("VerifyEmail");
        }

        return View("VerifyEmail", "Account");
    }


    //Generate Token and store in the token table
    private async Task HandleTokenGeneration(string userId, string purpose)
    {
        var user = await db.Users
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync();

        if (user != null)
        {
            string newToken = Guid.NewGuid().ToString();
            DateTime expiryDate = DateTime.Now.AddMinutes(5);

            var tokenEntity = new HotelRoomReservationSystem.Models.Token
            {
                token = newToken,
                Expiration = expiryDate,
                UsersId = user.Id,
                Purpose = purpose
            };

            db.Token.Add(tokenEntity);
            await db.SaveChangesAsync();

            string verificationUrl = Url.Action("VerifyEmail", "Account", new { token = newToken }, Request.Scheme);

            StringBuilder mailBody = new StringBuilder();
            mailBody.Append("<html><head></head><body>");
            mailBody.Append("<p>Dear User,</p>");
            switch (purpose)
            {
                case "ResendRegistration":
                case "REGISTER":
                    mailBody.Append($"<p>Please click <a href='{verificationUrl}'>here</a> to activate your account.</p>");
                    break;

                case "ResendResetPassword":
                case "forgot":
                    mailBody.Append($"<p>Please click <a href='{verificationUrl}'>here</a> to reset your password.</p>");

                    break;
                case "RECOVER":
                    mailBody.Append($"<p>Please click <a href='{verificationUrl}'>here</a> to recover your account.</p>");
                    break;
            }
            if (purpose == "RECOVER")
            {
                mailBody.Append("<p>Note: This link is valid for 2 days only.</p>");
            }
            else
            {
                mailBody.Append("<p>Note: This link is valid for 5 minutes only.</p>");
            }

            mailBody.Append("<p>Regards,</p>");
            mailBody.Append("<p>Hotel Team</p>");
            mailBody.Append("</body></html>");
            string subject = purpose switch
            {
                "ResendRegistration" => "Resend Verification - Activate Your Account",
                "REGISTER" => "Email Verification - Activate Your Account",
                "ResendResetPassword" => "Resend Verification - Reset Password",
                "forgot" => "Email Verification - Reset Password",
                "RECOVER" => "Resend Verification - Recover Account",
                _ => "Verification Email"
            };
            bool emailSent = SendEmail("yeapzijia1936@gmail.com", user.Email, subject, mailBody.ToString());

            if (emailSent)
            {
                TempData["Message"] = "Verification email sent successfully. Please check your email.";
                TempData["IsSuccess"] = true;
                ViewBag.tempUserToken = newToken;
            }
            else
            {
                TempData["Message"] = "Failed to send verification email.";
                TempData["IsSuccess"] = false;
            }
        }
        else
        {
            TempData["Message"] = "No users found in the database.";
            TempData["IsSuccess"] = false;
        }
    }

    //Email Verification Purpose
    //RESET PASSWORD AND Account Activation



    //EMAIL SETUP PURPOSE

    public bool SendEmail(string mailFrom, string Tomail, string sub, string body)
    {
        if (string.IsNullOrEmpty(mailFrom) || string.IsNullOrEmpty(Tomail))
        {
            Console.WriteLine("Invalid email address provided.");
            return false;
        }

        using (MailMessage mail = new MailMessage())
        {
            string displayName = _configuration["AppSettings:DisplayName"];
            mailFrom = _configuration["AppSettings:smtpUser"];

            mail.To.Add(Tomail.Trim());
            mail.From = new MailAddress(mailFrom, displayName);
            mail.Subject = sub.Trim();
            mail.Body = body.Trim();
            mail.IsBodyHtml = true;

            using (SmtpClient smtp = new SmtpClient())
            {
                smtp.Host = _configuration["AppSettings:smtpServer"];
                smtp.Port = Convert.ToInt32(_configuration["AppSettings:smtpPort"]);
                smtp.EnableSsl = Convert.ToBoolean(_configuration["AppSettings:EnableSsl"]);
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(
                    _configuration["AppSettings:smtpUser"],
                    _configuration["AppSettings:PWD"]
                );

                smtp.Timeout = 20000;
                smtp.Send(mail);
                Console.WriteLine("Email sent successfully.");
                return true;
            }
            return false;
        }

    }

    //EMAIL SETUP PURPOSE


    //GENERATE USERID
    private async Task<string> GenerateUserIdAsync()
    {
        string newUserId;

        do
        {
            string currentYear = DateTime.Now.Year.ToString().Substring(2);
            //DateTime.Now.Year.ToString().Substring(2);  

            var lastUser = db.Users
                .Where(u => u.Id.StartsWith($"U{currentYear}"))
                .OrderByDescending(u => u.Id)
                .FirstOrDefault();

            if (lastUser == null)
            {
                // If no user exists for the current year, start with 'U{year}000001'
                newUserId = $"U{currentYear}000001";
            }
            else
            {
                // Extract the numeric part, increment it, and format it
                string numericPart = lastUser.Id.Substring(5);  // Get the last 6 digits (after 'U{year}')
                int numericValue = int.Parse(numericPart);
                numericValue++;

                newUserId = $"U{currentYear}{numericValue.ToString("D6")}";
            }

            // Check if the generated ID already exists
            bool idExists = db.Users.Any(u => u.Id == newUserId);

            if (!idExists)
            {
                break;  // ID is unique, exit loop
            }

        } while (true);  // Continue looping until a unique ID is found

        return newUserId;
    }

    //GENERATE USERID




    //VALIDATE EMAIL AND PHONE EXIST OR NOT IN THE DATABASE
    //VALIDATE AGE BETWEEN 18 - 110

    public bool CheckEmailExist(string email)
    {

        return db.Users.Any(u => u.Email.ToLower() == email.ToLower());
    }

    public bool CheckPhoneExist(string phone)
    {

        return db.Users.Any(u => u.PhoneNum.ToLower() == phone.ToLower());
    }

    public bool CheckBirthDay(DateOnly birthDay)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - birthDay.Year;

        if (birthDay > today.AddYears(-age))
            age--;

        return age >= 18 && age <= 110;
    }

    //VALIDATE EMAIL AND PHONE EXIST OR NOT IN THE DATABASE
    //VALIDATE AGE BETWEEN 18 - 110


    //VALIDATION PURPOSE
    private bool ValidateField(Func<bool> condition, string key, string message)
    {
        if (condition())
        {
            ModelState.AddModelError(key, message);
            return true;
        }
        return false;
    }

    //VALIDATION PURPOSE


    //REUSABLE UPDATE PROFILE PURPOSE
    private bool ValidateUserDetails(UpdateUserVM model, HotelRoomReservationSystem.Models.Users existingUser)
    {
        bool hasErrors = false;

        if (existingUser.Email != model.Email && hp.CheckEmailExist(model.Email))
        {
            ModelState.AddModelError("Email", "This Email has been registered. Please try another.");
            hasErrors = true;
        }

        if (existingUser.PhoneNum != model.PhoneNum && hp.CheckPhoneExist(model.PhoneNum))
        {
            ModelState.AddModelError("PhoneNum", "This Phone Number has been registered. Please try another.");
            hasErrors = true;
        }

        if (model.Photo != null)
        {
            string validationError = hp.ValidatePhoto(model.Photo);
            if (!string.IsNullOrEmpty(validationError))
            {
                ModelState.AddModelError("", validationError);
                hasErrors = true;
            }
        }

        if (!string.IsNullOrEmpty(model.Base64Photo))
        {
            string validationError = hp.ValidatePhoto(null, model.Base64Photo);
            if (!string.IsNullOrEmpty(validationError))
            {
                ModelState.AddModelError("Photo", validationError);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private void UpdateExistingUser(HotelRoomReservationSystem.Models.Users existingUser, UpdateUserVM model)
    {
        existingUser.Name = model.FirstName + " " + model.LastName;
        existingUser.Email = model.Email;
        existingUser.PhoneNum = model.PhoneNum;
        existingUser.DOB = model.BirthDay;

        if (model.Photo != null || !string.IsNullOrEmpty(model.Base64Photo))
        {
            hp.DeletePhoto(existingUser.Portrait, "/images/user_photo/");
        }


        if (!string.IsNullOrEmpty(model.Base64Photo))
        {
            existingUser.Portrait = hp.SavePhoto(null, model.Base64Photo, "/images/user_photo/", existingUser.Id);
        }
        else if (model.Photo != null)
        {
            existingUser.Portrait = hp.SavePhoto(model.Photo, null, "/images/user_photo/", existingUser.Id);
        }
    }

    private void UpdateUserClaims(HotelRoomReservationSystem.Models.Users existingUser, ClaimsIdentity claimsIdentity)
    {
        var updatedClaims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, existingUser.Email),
        new Claim("Names", existingUser.Name),
        new Claim("Portrait", existingUser.Portrait),
    };

        claimsIdentity.RemoveClaim(claimsIdentity.FindFirst("Portrait"));
        claimsIdentity.RemoveClaim(claimsIdentity.FindFirst("Names"));
        claimsIdentity.AddClaims(updatedClaims);

        HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity));
    }

    private void PrepareModelForReturn(UpdateUserVM model, HotelRoomReservationSystem.Models.Users existingUser)
    {
        var nameParts = existingUser.Name.Split(' ');
        model.id = existingUser.Id;
        model.FirstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
        model.LastName = string.Join(" ", nameParts.Skip(1));
        model.Email = existingUser.Email;
        model.portrait = existingUser.Portrait;
        model.PhoneNum = existingUser.PhoneNum;
        model.BirthDay = existingUser.DOB;
    }

    private HotelRoomReservationSystem.Models.Users GetUserByEmail(string email)
    {
        return db.Users.FirstOrDefault(u => u.Email == email);
    }

    private UpdateUserVM GetUsersModels(string userId)
    {
        var user = db.Users.FirstOrDefault(u => u.Id == userId);

        if (user == null)
        {
            return null; // Return null if the user is not found
        }

        var nameParts = user.Name?.Split(' ') ?? Array.Empty<string>();
        var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
        var lastName = string.Join(" ", nameParts.Skip(1));

        var userDetails = new UpdateUserVM
        {
            id = user.Id,
            FirstName = firstName,
            LastName = lastName,
            Email = user.Email,
            PhoneNum = user.PhoneNum,
            portrait = user.Portrait,
            BirthDay = user.DOB
        };

        return userDetails;
    }

    //REUSABLE UPDATE PROFILE PURPOSE



    //DELETE ACCOUNT
    [HttpGet]
    [Authorize(Roles = "Customer")]
    public IActionResult deleteAccount()
    {
        var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
        var user = GetUserByEmail(userEmail);

        var hasNonCanceledFutureReservation = db.Reservation.Any(r =>
        r.UsersId == user.Id &&
        r.CheckInDate > DateTime.Now &&
        r.Status != "Canceled"
        );

        if (hasNonCanceledFutureReservation)
        {
            return Json(new { success = false, message = "You cannot delete your account as you have upcoming reservations that are not canceled." });
        }
        else
        {
            user.Status = "DELETED";
            db.SaveChanges();
            return Json(new { success = true, message = "Your account has been successfully deleted. Now system will log out your account to complete the delete process. Thank you for using our service!" });
        }
    }



    //ASSISTED ACCOUNT RECOVERY 

    [AllowAnonymous]

    public IActionResult AccountRecovery()
    {
        if (User.Identity.IsAuthenticated)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "Manager" || userRole == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (userRole == "Customer")
            {
                return RedirectToAction("Index", "Home");
            }
        }
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public IActionResult AccountRecovery(forgotPasswordVM model)
    {
        var u = db.Users.FirstOrDefault(user => user.Email == model.Email);
        if (u == null)
        {
            ModelState.AddModelError("", "This Email has not been registered.");
            return View(model);
        }

        if (u.Status != "DELETED")
        {
            ModelState.AddModelError("", "This account has not been deleted.");
            return View(model);
        }

        if (ModelState.IsValid)
        {
            u.Status = "REQUESTED";
            db.SaveChanges();
            TempData["RecoveryMessage"] = "Your recovery request has been sent to the admin. Please allow up to 3 working days for approval. Once approved, you will receive a verification email.";
            RedirectToAction("AccountRecovery");
        }

        return View(model);
    }



    //SEND SMS FOR MFA

    private bool SendSms(string otp, string phoneNumber)
    {
        try
        {
            Console.WriteLine(otp);
            Console.WriteLine(phoneNumber);

            var message = MessageResource.Create(
                body: $"Hello! This is your OTP: {otp}. It is valid for 5 minutes only.",
                from: new PhoneNumber("+12184001863"), // Replace with your Twilio phone number
                to: new PhoneNumber(phoneNumber), // Replace with the recipient's phone number
                client: _client
            );

            if (message.ErrorCode != null)
            {
                Console.WriteLine($"Failed to send SMS: {message.ErrorMessage}");
                return false; // Error occurred
            }

            Console.WriteLine("SMS sent successfully!");
            return true; // SMS sent successfully
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return false; // Exception occurred
        }
        //Console.WriteLine(otp);
        //Console.WriteLine(phoneNumber);
        //var message = MessageResource.Create(
        //    body: $"Hello This Your otp number {otp}, this otp valid 5 minute only!",
        //    from: new PhoneNumber("+12184001863"), // Replace with your Twilio phone number
        //    to: new PhoneNumber(phoneNumber), // Replace with the recipient's phone number
        //    client: _client
        //);
        //return Task.CompletedTask;
    }


    [HttpPost]
    [Authorize(Roles = "Customer")]
    public IActionResult UpdateMFAStatus(string status)
    {
        var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
        var user = GetUserByEmail(userEmail);

        if (string.IsNullOrEmpty(status))
        {
            return Json(new { success = false, message = "Invalid status value provided." });
        }



        var mfaRecord = db.MFALogin.FirstOrDefault(m => m.UsersId == user.Id);

        if (mfaRecord == null)
        {
            return Json(new { success = false, message = "MFA record not found for the user." });
        }


        mfaRecord.status = status;
        db.SaveChanges();

        return Json(new { success = true, message = "MFA status updated successfully." });
    }


    [AllowAnonymous]
    public IActionResult mfaPage()
    {
        if (User.Identity.IsAuthenticated)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "Manager" || userRole == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (userRole == "Customer")
            {
                return RedirectToAction("Index", "Home");
            }
        }

        string userId = HttpContext.Session.GetString("UserIdForMFA");


        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var user = db.Users.FirstOrDefault(u => u.Id == userId);

        Random random = new Random();
        string otp = random.Next(100000, 999999).ToString();

        DateTime expiryDateTime = DateTime.Now.AddMinutes(5);

        var mfaRecord = db.MFALogin.FirstOrDefault(m => m.UsersId == userId);

        if (mfaRecord != null)
        {
            mfaRecord.otp = otp;
            mfaRecord.ExipredDateTime = expiryDateTime;
            db.SaveChanges();
        }
        var phoneNumber = "+6" + user.PhoneNum;
        var isSmsSent = SendSms(otp, phoneNumber);
        if (!isSmsSent)
        {
            TempData["errorPhone"] = "This Phonue Number Temporary Unavialble for send otp. Please contact admin through contact page.";
            return RedirectToAction("Login");
        }

        return View();
    }

    [HttpPost]
    public IActionResult mfaPage(mfaVM model)
    {
        string userId = HttpContext.Session.GetString("UserIdForMFA");
        var rememberMe = HttpContext.Session.GetString("MyBoolKey");

        bool result = false;
        if (!string.IsNullOrEmpty(rememberMe))
        {
            result = bool.Parse(rememberMe);
        }

        var u = db.Users.FirstOrDefault(u => u.Id == userId);

        if (ModelState.IsValid)
        {
            var mfaRecord = db.MFALogin.FirstOrDefault(m => m.UsersId == userId);

            if (mfaRecord != null)
            {
                if (mfaRecord.ExipredDateTime > DateTime.Now && mfaRecord.otp == model.oneTimePassword)
                {
                    HttpContext.Session.Remove("UserIdForMFA");
                    HttpContext.Session.Remove("MyBoolKey");
                    hp.SignIn(u!.Email, u.Role, result, u.Name, u.Portrait);
                    var userJson = JsonSerializer.Serialize(u);
                    HttpContext.Session.SetString("User", userJson);

                    TempData["loginSuccess"] = $" ✅ Welcome {u.Name}, login successful.";
                    if (u.Role == "Admin" || u.Role == "Manager")
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError("oneTimePassword", "Invalid or expired OTP.");
                }
            }
            else
            {
                ModelState.AddModelError("UsersId", "No MFA record found for the user.");
            }
        }
        else
        {
            ModelState.AddModelError("", "Invalid input data.");
        }


        return View(model);
    }




    //[HttpGet("Account/ResendVerification/{token}")]
    //public async Task<IActionResult> ResendVerification(string token)
    //{
    //    if (string.IsNullOrEmpty(token))
    //    {
    //        ViewBag.Message = "Invalid token.";
    //        ViewBag.IsSuccess = false;
    //        return View("VerifyEmail");
    //    }

    //    var tokenEntity = await db.Token
    //        .Where(t => t.token == token)
    //        .FirstOrDefaultAsync();

    //    if (tokenEntity == null)
    //    {
    //        ViewBag.Message = "Token not found.";
    //        ViewBag.IsSuccess = false;
    //        return View("VerifyEmail");
    //    }
    //    var user = await db.Users.FindAsync(tokenEntity.UsersId);
    //    // Check if the token has expired
    //    if (DateTime.Now > tokenEntity.Expiration)
    //    {
    //        ViewBag.Message = "Token has expired.";
    //        ViewBag.UserId = user.Id;
    //        ViewBag.IsSuccess = false;
    //        return View("VerifyEmail");
    //    }
    //    if (ViewBag.Purpose != null && ViewBag.Purpose != "")
    //    {
    //        Console.WriteLine(ViewBag.Purpose);
    //    }
    //    else
    //    {
    //        Console.WriteLine("ViewBag.Purpose is null or empty.");
    //    }
    //    //ViewBag.Purpose = tokenEntity.Purpose;
    //    if (tokenEntity.Purpose == "ResendResetPassword")
    //    {
    //        TempData["UserId"] = user.Id;
    //        Console.WriteLine("ViewBag.UserId set to: " + ViewBag.UserId);
    //        return RedirectToAction("resetPassword");
    //    }
    //    else if (tokenEntity.Purpose == "ResendRegistration")
    //    {
    //        if (user != null)
    //        {
    //            // user.Roles = "Verified"; 
    //            user.Status = "VERIFY";
    //            db.Users.Update(user);
    //            await db.SaveChangesAsync();

    //            ViewBag.UserId = user.Id;
    //            ViewBag.Message = "Email verification successful.";
    //            ViewBag.IsSuccess = true;
    //            return View("VerifyEmail");
    //        }
    //    }

    //    ViewBag.Message = "User not found.";
    //    ViewBag.IsSuccess = false;
    //    return View("VerifyEmail");
    //    //if (string.IsNullOrEmpty(userId))
    //    //{
    //    //    ViewBag.Message = "User ID is required.";
    //    //    ViewBag.IsSuccess = false;
    //    //    return RedirectToAction("Index"); 
    //    //}

    //    //var user = await db.Users.FindAsync(userId);
    //    //if (user == null)
    //    //{
    //    //    ViewBag.Message = "User not found.";
    //    //    ViewBag.IsSuccess = false;
    //    //    return RedirectToAction("Index");
    //    //}

    //    //// Generate a new verification token
    //    //string newToken = Guid.NewGuid().ToString();
    //    //DateTime expiryDate = DateTime.Now.AddMinutes(5);

    //    //var tokenEntity = new HotelRoomReservationSystem.Models.Token
    //    //{
    //    //    token = newToken,
    //    //    Expiration = expiryDate,
    //    //    UsersId = userId
    //    //};

    //    //db.Token.Add(tokenEntity);
    //    //await db.SaveChangesAsync();



    //    //string verificationUrl = Url.Action("VerifyEmail", "Account", new { token = newToken }, Request.Scheme);
    //    //// Build the email body

    //    //StringBuilder mailbody = new StringBuilder(" ");
    //    //mailbody.Append("<html><head></head><body>");
    //    //mailbody.Append("<p>Dear User,</p>");
    //    //mailbody.Append($"<p>Please click <a href='{verificationUrl}'>here</a> to activate your account.</p>");
    //    //mailbody.Append("<p>Note: This link is valid for 5 minutes only.</p>");
    //    //mailbody.Append("<p>Regards,</p>");
    //    //mailbody.Append("<p>Your Team</p>");
    //    //mailbody.Append("</body></html>");

    //    //bool emailSent = SendEmail("yeapzijia1936@gmail.com", user.Email, "Resend Verification", mailbody.ToString());
    //    //if (emailSent)
    //    //{
    //    //    ViewBag.Message = "Verification email sent successfully.";
    //    //    ViewBag.IsSuccess = true;
    //    //}
    //    //else
    //    //{
    //    //    ViewBag.Message = "Failed to send verification email.";
    //    //    ViewBag.IsSuccess = false;
    //    //}

    //    //return RedirectToAction("Index"); // Redirect to a relevant page after sending the email
    //}





    //public static string HashPassword(string password)
    //{
    //    return BCrypt.Net.BCrypt.HashPassword(password);
    //}

    //public static bool VerifyPassword(string password, string hashedPassword)
    //{
    //    return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    //}
}
