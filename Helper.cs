using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Demo;

public class Helper
{
    private readonly IWebHostEnvironment en;
    private readonly IHttpContextAccessor ct;
    private readonly IConfiguration cf;
    private readonly HotelRoomReservationDB db;

    public Helper(IWebHostEnvironment en, HotelRoomReservationDB db, IHttpContextAccessor ct,
                  IConfiguration cf)
    {
        this.en = en;
        this.ct = ct;
        this.cf = cf;
        this.db = db;
    }

    //Photo Upload
    public string ValidateFiles(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return "No files were uploaded.";
        }
        // Check total number of files
        if (files.Count > 5)
        {
            return "You can only upload a maximum of 5 files.";
        }

        var allowedContentTypes = new Regex(@"^(image\/(jpeg|png|jpg)|video\/(mp4|mov|avi))$", RegexOptions.IgnoreCase);
        var allowedFileExtensions = new Regex(@"\.(jpeg|jpg|png|mp4|mov|avi)$", RegexOptions.IgnoreCase);

        foreach (var file in files)
        {
            // Check file type
            if (!allowedContentTypes.IsMatch(file.ContentType) || !allowedFileExtensions.IsMatch(file.FileName))
            {
                return $"File `{file.FileName}` is not a valid image or video file.";
            }

            // Check file size (maximum 2 MB per file)
            if (file.Length > 2 * 1024 * 1024)
            {
                return $"File '{file.FileName}' exceeds the maximum allowed size of 2 MB.";
            }
        }

        return "";
    }

    public string ValidatePhoto(IFormFile file, string base64Photo = null)
    {
        var reType = new Regex(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

        if (file == null && string.IsNullOrWhiteSpace(base64Photo))
        {
            return "Photo is required.";
        }

        if (file != null)
        {
            if (file.Length == 0 || !reType.IsMatch(file.ContentType) || !reName.IsMatch(file.FileName))
            {
                return "Only JPG and PNG photos are allowed.";
            }
            else if (file.Length > 1 * 1024 * 1024)
            {
                return "Photo size cannot exceed 1MB.";
            }
        }

        if (!string.IsNullOrWhiteSpace(base64Photo))
        {
            // Ensure the Base64 string contains valid image data
            if (!base64Photo.StartsWith("data:image/jpeg;base64,") && !base64Photo.StartsWith("data:image/png;base64,"))
            {
                return "Invalid photo format.";
            }
        }

        return "";
    }
    //public string SaveImage(IFormFile f, string folder, string fileName)
    //{
    //    // Validate file type
    //    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
    //    var fileExtension = Path.GetExtension(f.FileName).ToLowerInvariant();

    //    if (!allowedExtensions.Contains(fileExtension))
    //    {
    //        throw new InvalidOperationException("Only JPG, JPEG, and PNG file types are allowed.");
    //    }

    //    string directoryPath = Path.Combine(en.WebRootPath, folder);

    //    if (!Directory.Exists(directoryPath))
    //    {
    //        Directory.CreateDirectory(directoryPath);
    //    }

    //    // Generate a unique file name with the original extension

    //    var path = Path.Combine(directoryPath, fileName);

    //    // Resize and save the image
    //    //var options = new ResizeOptions
    //    //{
    //    //    Size = new(200, 200),
    //    //    Mode = ResizeMode.Crop,
    //    //};

    //    using var stream = f.OpenReadStream();
    //    using var img = Image.Load(stream);
    //    //img.Mutate(x => x.Resize(options));
    //    img.Save(path); // Automatically chooses the correct encoder based on extension

    //    return fileName;
    //}

    public string SaveImage(IFormFile f, string folder, string fileName)
    {
        // Allowed file extensions
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

        // Validate file type from content (MIME type)
        string fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        var mimeType = f.ContentType.ToLowerInvariant();

        // If file name does not contain an extension, add one based on MIME type
        if (string.IsNullOrEmpty(fileExtension))
        {
            if (mimeType.Contains("jpeg")) fileExtension = ".jpg";
            else if (mimeType.Contains("png")) fileExtension = ".png";
            else
            {
                throw new InvalidOperationException("Unsupported file type.");
            }

            fileName += fileExtension; // Append the determined extension
        }
        else if (!allowedExtensions.Contains(fileExtension))
        {
            throw new InvalidOperationException("Invalid file extension.");
        }

        // Ensure directory exists
        string directoryPath = Path.Combine(en.WebRootPath, folder);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string path = Path.Combine(directoryPath, fileName);

        // Resize and save the image
        using var stream = f.OpenReadStream();
        using var img = Image.Load(stream);

        img.Save(path); // Automatically chooses encoder based on file extension

        return fileName;
    }

    public string SavePhoto(IFormFile file, string base64Photo, string folder, string userID)
    {
        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(userID))
        {
            throw new ArgumentException("Folder path and user ID cannot be null or empty.");
        }

        var fileName = $"{userID}.jpg";
        var folderPath = Path.Combine(en.WebRootPath, folder.TrimStart('/')); // Ensure valid folder path
        var filePath = Path.Combine(folderPath, fileName);

        // Ensure the folder exists
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var options = new ResizeOptions
        {
            Size = new(250, 250),
            Mode = ResizeMode.Crop,
        };

        try
        {
            if (file != null)
            {
                // Process the IFormFile
                using var stream = file.OpenReadStream();
                using var img = Image.Load(stream);
                img.Mutate(x => x.Resize(options)); // Resize the image
                img.Save(filePath); // Save the image
            }
            else if (!string.IsNullOrWhiteSpace(base64Photo))
            {
                // Decode Base64 string and save as a file
                var base64Data = base64Photo.Substring(base64Photo.IndexOf(",") + 1); // Remove metadata (data:image/...;base64,)
                var imageBytes = Convert.FromBase64String(base64Data);

                using var img = Image.Load(imageBytes);
                img.Mutate(x => x.Resize(options)); // Resize the image
                img.Save(filePath); // Save the image
            }
            else
            {
                throw new ArgumentException("Both file and base64Photo are null or empty.");
            }
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            Console.WriteLine($"Error saving photo: {ex.Message}");
            throw;
        }

        return fileName;
    }
    public void DeletePhoto(string file, string folder)
    {
        if (file != "default_profile.jpg")
        {
            file = Path.GetFileName(file);
            var path = Path.Combine(en.WebRootPath, folder.TrimStart('/'), file);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path); // Delete the file
                    Console.WriteLine($"File deleted successfully: {path}");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("Access denied. Ensure the application has permission to delete the file.");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"File is in use or locked: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error while deleting file: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"File not found: {path}");
            }
        }
    }

    //public void DeletePhoto(string file, string folder)
    //{
    //    // TODO
    //    file = Path.GetFileName(file);
    //    var path = Path.Combine(en.WebRootPath, folder, file);
    //    File.Delete(path);
    //}


    // ------------------------------------------------------------------------
    // Email Helper Functions
    // ------------------------------------------------------------------------

    public void SendEmail(MailMessage mail)
    {
        // TODO
        string user = cf["AppSettings:smtpUser"] ?? "";
        string pass = cf["AppSettings:PWD"] ?? "";
        string name = cf["AppSettings:DisplayName"] ?? "";
        string host = cf["AppSettings:smtpServer"] ?? "";
        int port = cf.GetValue<int>("AppSettings:smtpPort");

        mail.From = new MailAddress(user, name);
        // TODO

        using var smtp = new SmtpClient
        {
            Host = host,
            Port = port,
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass),
        };

        try
        {
            smtp.Send(mail);
        }
        catch (SmtpException)
        {

        }
        //Console.WriteLine($"{user} {pass} {name} {host} {port}");
    }

    public void SendInvoiceEmail(string userEmail, string userName, byte[] pdfBytes, string transactionId)
    {
        // Construct the email
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(userEmail, userName));
        mail.Subject = "Payment Confirmation - Invoice Attached";
        mail.Body = "Thank you for your payment. Please find your invoice attached.";
        mail.IsBodyHtml = true;

        // Attach the invoice
        var attachment = new Attachment(new MemoryStream(pdfBytes), $"Invoice_{transactionId}.pdf");
        mail.Attachments.Add(attachment);

        // Use the existing SendEmail method
        SendEmail(mail);
    }


    // ------------------------------------------------------------------------
    // Security Helper Functions
    // ------------------------------------------------------------------------

    private readonly PasswordHasher<object> ph = new();

    //public string HashPassword(string password)
    //{
    //    return ph.HashPassword(0, password);
    //}

    //public bool VerifyPassword(string hash, string password)
    //{
    //    // TODO
    //    return ph.VerifyHashedPassword(0,hash,password) == PasswordVerificationResult.Success;
    //}

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }

    public void SignIn(string email, string role, bool rememberMe, string name, string picture)
    {
        // (1) Claim, identity and principal

        List<Claim> claims = [
            new(ClaimTypes.Name,email),
            new Claim("Names", name),
            new Claim("Portrait", picture),
            new(ClaimTypes.Role,role),
         ];



        ClaimsIdentity identity = new(claims, "Cookies");


        ClaimsPrincipal principal = new(identity);

        // (2) Remember me (authentication properties)
        AuthenticationProperties authProps = new AuthenticationProperties();

        if (rememberMe)
        {
            authProps.IsPersistent = true;
            authProps.ExpiresUtc = DateTime.Now.AddDays(30);
        }

        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,


        };

        // (3) Sign in

        ct.HttpContext!.SignInAsync(principal, authProps);
    }

    public void SignOut()
    {
        ct.HttpContext!.SignOutAsync();
    }

    public async Task<string> GenerateUserIdAsync()
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

    public bool CheckEmailExist(string email)
    {

        return db.Users.Any(u => u.Email.ToLower() == email.ToLower());
    }

    public bool CheckPhoneExist(string phone)
    {

        return db.Users.Any(u => u.PhoneNum.ToLower() == phone.ToLower());
    }

    //private bool ValidateField(Func<bool> condition, string key, string message)
    //{
    //    if (condition())
    //    {
    //        ModelState.AddModelError(key, message);
    //        return true;
    //    }
    //    return false;
    //}

    public bool CheckBirthDay(DateOnly birthDay)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - birthDay.Year;

        if (birthDay > today.AddYears(-age))
            age--;

        return age >= 18 && age <= 110;
    }

    public static string GetLevelInfo(int loyaltyPoints)
    {
        string level = "Elite"; // Default level
        string progressClass = "";
        int levelPoint = 0;

        if (loyaltyPoints <= 1000)
        {
            level = "Basic";
            progressClass = "completed";
            levelPoint = 1000;
        }
        else if (loyaltyPoints >= 1000)
        {
            level = "Platinum";
            progressClass = "completed";
            levelPoint = 2500;
        }
        else if (loyaltyPoints >= 2500)
        {
            level = "VIP";
            progressClass = "completed";
            levelPoint = 5000;
        }

        // Use StringBuilder for efficient string concatenation
        StringBuilder sb = new StringBuilder();
        sb.Append("<div class='level-info'>");
        sb.Append($"<span>{level}</span> - {loyaltyPoints} / {levelPoint}");
        sb.Append("</div>");

        return sb.ToString();
    }


    ///////////////////////////////////////////////////////////////////////////////////
    ///Check available 
    ///////////////////////////////////////////////////////////////////////////////////
    public void CheckWaitingList()
    {
        var achieve = db.WaitingList.Where(wt => DateTime.Now >= wt.DatePerform).ToList();
        foreach (var work in achieve)
        {
            var oldRoom = db.Rooms.Where(rm => rm.Id == work.RoomId).FirstOrDefault();
            if (oldRoom != null)
            {
                if (string.Equals(work.Action, "Replace", StringComparison.OrdinalIgnoreCase))
                {
                    if (work.NewRoomTypeId != null)
                    {
                        oldRoom.Status = "Available";
                        oldRoom.RoomTypeId = work.NewRoomTypeId;
                    }
                }
                else if (string.Equals(work.Action, "Disabled", StringComparison.OrdinalIgnoreCase))
                {
                    oldRoom.Status = "Disabled";
                        
                }
                db.Rooms.Update(oldRoom);
                db.WaitingList.Remove(work);
            }
        }
        db.SaveChanges();
    }

}

public static class SessionExtensions
{
    public static T GetObjectFromJson<T>(this ISession session, string key)
    {
        var json = session.GetString(key);
        return json == null ? default : JsonConvert.DeserializeObject<T>(json);
    }

    public static void SetObjectAsJson(this ISession session, string key, object value)
    {
        session.SetString(key, JsonConvert.SerializeObject(value));
    }

}
