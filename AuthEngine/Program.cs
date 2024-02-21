using Microsoft.EntityFrameworkCore;
using AuthEngine.Models;
using AuthEngine.Data;
using AuthEngine.Util;

var builder = WebApplication.CreateBuilder(args);

string? connectionString = builder.Configuration.GetConnectionString("MySQL");
if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("MySQL connection string is not found");
}
Console.WriteLine(connectionString);
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<CredentialContext>(options => options.UseMySQL(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseHsts();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<CredentialContext>();
    if (context.Database.GetPendingMigrations().Any())
    {
        context.Database.Migrate();
    }
}

app.MapGet("/", () => "Hello World!");

// InternalServerError = 1000 Something unexpected happened on the server-side.
// BadRequest = 1001 The request was invalid or could not be understood by the server.
// UnsupportedFeature = 1002 The server does not support the requested feature.
// Unauthorized = 2000 The user is not authorized to perform the requested action.
// InvalidCredentials = 2001 Username or password is incorrect.
// DisabledAccount = 2002 The user's account is disabled and cannot be accessed.
// ExpiredToken = 2003 The provided authentication token is no longer valid
// InvalidToken = 2004 The provided authentication token is invalid or tampered with.
// TokenRequired = 2005 An authentication token is required to access this resource.
// Forbidden = 3000 The user is not allowed to access the requested resource.
// UserNotFound = 4000 The requested user was not found.
// UserExists = 4001 The requested user already exists.
// UserDisabled = 4002 The requested user account is disabled and cannot be accessed.
// UserNotAuthenticated = 4003 The user is not authenticated and cannot be accessed.
// UserNotAuthorized = 4004 The user is not authorized to access the requested resource.
// UserNotActive = 4005 The user is not active and cannot be accessed.
// Success = 5000 The request was successful.
// Completed = 5001 The requested action was completed successfully.
// Accepted = 5002 The request was accepted and will be processed.
// NoContent = 5003 The request was successful but no content was returned.
// Authenticated = 5004 The user is authenticated and can access the requested resource.
// Authorized = 5005 The user is authorized to access the requested resource.
// Active = 5006 The user is active and can be accessed.

app.MapGet("/status", (CredentialContext context) =>
{
    try
    {
        return Results.Ok("Service is running");
    }
    catch (System.Exception)
    {
        return Results.Problem(title: "Internal Server Error", statusCode: statusCode.InternalServerError, detail: "An error occurred while processing your request. Please try again later.");
    }
});

/*
    Endpoint - /register
    Method - GET
    Parameters - username, password, firstName, lastName, email, phoneNumber, IsAdmin, IsDisabled
    Description - Registers a new user
    ReturnCodes -
        200 OK => User registered successfully
        1000 InternalServerError => An error occurred while processing your request. Please try again later.
        4001 UserExists => The username already exists. Please choose a different username.
    Status - Tested OK
*/
app.MapGet("/register", (CredentialContext context, string username, string password, string firstName, string lastName, string email, string phoneNumber, bool IsAdmin = false, bool IsDisabled = false) =>
{
    try
    {
        Credentials? userExists = context.Credentials.Where(c => c.Username == username).FirstOrDefault();
        if (userExists != null)
        {
            return Results.Problem(title: "User Already Exists", statusCode: statusCode.UserExists, detail: "The username already exists. Please choose a different username.");
        }
        else
        {
            string userId = Crypto.genSHA256(username + password + DateTime.Now);
            var user = new UserInfo { UserId = userId, FirstName = firstName, LastName = lastName, Email = email, PhoneNumber = phoneNumber };
            var credential = new Credentials { UserId = userId, Username = username, Password = password, CreatedAt = DateTime.Now, IsAdmin = IsAdmin, IsDisabled = IsDisabled };
            context.UserInfo.Add(user);
            context.Credentials.Add(credential);
            context.SaveChanges();
            return Results.Ok("User registered successfully");
        }

    }
    catch (System.Exception)
    {
        return Results.Problem(title: "Internal Server Error", statusCode: statusCode.InternalServerError, detail: "An error occurred while processing your request. Please try again later.");
    }
});

/*
    Endpoint - /authenticate
    Method - GET
    Parameters - username, password
    Description - Authenticates a user and returns a token
    ReturnCodes -
        200 OK => Token
        1000 InternalServerError => An error occurred while processing your request. Please try again later.
        2001 InvalidCredentials => The username or password is incorrect. Please try again.
        4002 UserDisabled => User account is disabled. Please contact the administrator for further assistance.
    Status - Tested OK
*/
app.MapGet("/authenticate", (CredentialContext context, string username, string password) =>
{
    try
    {
        Credentials? user = context.Credentials.Where(c => c.Username == username && c.Password == password).FirstOrDefault();
        if (user != null)
        {
            if (user.IsDisabled == true)
            {
                return Results.Problem(title: "User Account Disabled", statusCode: statusCode.UserDisabled, detail: "User account is disabled. Please contact the administrator for further assistance.");
            }
            else
            {
                user.LastLogin = DateTime.Now;
                user.Token = Crypto.genSHA256(username + password + DateTime.Now);
                user.TokenExpires = DateTime.Now.AddSeconds(3600);
                context.SaveChanges();
                return Results.Ok(user.Token);
            }
        }
        else
        {
            // return Results.NotFound("User not found");
            return Results.Problem(title: "Invalid Credentials", statusCode: statusCode.InvalidCredentials, detail: "The username or password is incorrect. Please try again.");
        }
    }
    catch (System.Exception)
    {
        return Results.Problem(title: "Internal Server Error", statusCode: statusCode.InternalServerError, detail: "An error occurred while processing your request. Please try again later.");
    }
});

/*
    Endpoint - /logout
    Method - GET
    Parameters - token
    Description - Logs out a user
    ReturnCodes -
        200 OK => User logged out successfully
        1000 InternalServerError => An error occurred while processing your request. Please try again later.
        2003 ExpiredToken => Token has expired. Please authenticate again.
        2004 InvalidToken => The token is invalid. Please authenticate again.
        4002 UserDisabled => User account is disabled. Please contact the administrator for further assistance.
    Status - Tested OK
*/
app.MapGet("/logout", (CredentialContext context, string token) =>
{
    try
    {
        Credentials? user = context.Credentials.Where(c => c.Token == token).FirstOrDefault();
        if (user != null)
        {
            if (user.IsDisabled == true)
            {
                return Results.Problem(title: "User Disabled", statusCode: statusCode.UserDisabled, detail: "User is disabled. Please contact the administrator for further assistance.");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Results.Problem(title: "Token Expired", statusCode: statusCode.ExpiredToken, detail: "Token has expired. Please authenticate again.");
            }
            else
            {
                user.Token = "";
                user.TokenExpires = DateTime.Now;
                context.SaveChanges();
                return Results.Ok("User logged out successfully");
            }
        }
        else
        {
            // return Results.NotFound("Token Not Found");
            return Results.Problem(title: "Token Not Found", statusCode: statusCode.InvalidToken, detail: "The token is invalid. Please authenticate again.");
        }
    }
    catch (System.Exception)
    {
        return Results.Problem(title: "Internal Server Error", statusCode: statusCode.InternalServerError, detail: "An error occurred while processing your request. Please try again later.");
    }
});

/*
    Endpoint - /validateToken
    Method - GET
    Parameters - token
    Description - Validates a token
    ReturnCodes -
        200 OK => Token is valid
        1000 InternalServerError => An error occurred while processing your request. Please try again later.
        2003 ExpiredToken => Token has expired. Please authenticate again.
        2004 InvalidToken => The token is invalid. Please authenticate again.
        4002 UserDisabled => User account is disabled. Please contact the administrator for further assistance.
    Status - Tested OK
*/
app.MapGet("/validateToken", (CredentialContext context, string token) =>
{
    try
    {
        Credentials? user = context.Credentials.Where(c => c.Token == token).FirstOrDefault();
        if (user != null)
        {
            if (user.IsDisabled == true)
            {
                return Results.Problem(title: "User Disabled", statusCode: statusCode.UserDisabled, detail: "User is disabled. Please contact the administrator for further assistance.");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Results.Problem(title: "Token Expired", statusCode: statusCode.ExpiredToken, detail: "Token has expired. Please authenticate again.");
            }
            else
            {
                return Results.Ok("Token is valid");
            }
        }
        else
        {
            // return Results.NotFound("Token Not Found"); 
            return Results.Problem(title: "Token Not Found", statusCode: statusCode.InvalidToken, detail: "The token is invalid. Please authenticate again.");
        }
    }
    catch (System.Exception)
    {
        return Results.Problem(title: "Internal Server Error", statusCode: statusCode.InternalServerError, detail: "An error occurred while processing your request. Please try again later.");
    }
});

/*
    Endpoint - /renewToken
    Method - GET
    Parameters - token
    Description - Renews a token
    ReturnCodes -
        200 OK => New Token
        1000 InternalServerError => An error occurred while processing your request. Please try again later.
        2003 ExpiredToken => Token has expired. Please authenticate again.
        2004 InvalidToken => The token is invalid. Please authenticate again.
        4002 UserDisabled => User account is disabled. Please contact the administrator for further assistance.
    Status - Tested OK

*/
app.MapGet("/renewToken", (CredentialContext context, string token) =>
{
    try
    {
        Credentials? user = context.Credentials.Where(c => c.Token == token).FirstOrDefault();
        if (user != null)
        {
            if (user.IsDisabled == true)
            {
                return Results.Problem(title: "User Disabled", statusCode: statusCode.UserDisabled, detail: "User is disabled. Please contact the administrator for further assistance.");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Results.Problem(title: "Token Expired", statusCode: statusCode.ExpiredToken, detail: "Token has expired. Please authenticate again.");
            }
            else
            {
                user.Token = Crypto.genSHA256(user.Username + user.Password + DateTime.Now);
                user.TokenExpires = DateTime.Now.AddSeconds(120);
                context.SaveChanges();
                return Results.Ok(user.Token);
            }
        }
        else
        {
            // return Results.NotFound("Token Not Found");
            return Results.Problem(title: "Token Not Found", statusCode: statusCode.InvalidToken, detail: "The token is invalid. Please authenticate again.");
        }
    }
    catch (System.Exception)
    {
        return Results.Problem(title: "Internal Server Error", statusCode: statusCode.InternalServerError, detail: "An error occurred while processing your request. Please try again later.");
    }
});

/*
    Endpoint - /disableUser
    Method - GET
    Parameters - username, token
    Description - Disables a user
    ReturnCodes -
        200 OK => User disabled successfully
        1000 InternalServerError => An error occurred while processing your request. Please try again later.
        2003 ExpiredToken => Token has expired. Please authenticate again.
        2004 InvalidToken => The token is invalid. Please authenticate again.
        3000 Forbidden => You don't have permission to perform this action.
        4000 UserNotFound => The user was not found.
        4002 UserDisabled => User account is disabled. Please contact the administrator for further assistance.
    Status - Tested OK
*/
app.MapGet("/disableUser", (CredentialContext context, string username, string token) =>
{
    try
    {
        Credentials? user = context.Credentials.Where(c => c.Token == token).FirstOrDefault();
        if (user != null)
        {
            if (user.IsDisabled == true)
            {
                return Results.Problem(title: "User Disabled", statusCode: statusCode.UserDisabled, detail: "User is disabled. Please contact the administrator for further assistance.");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Results.Problem(title: "Token Expired", statusCode: statusCode.ExpiredToken, detail: "Token has expired. Please authenticate again.");
            }
            else
            {
                if (user.IsAdmin == true)
                {
                    Credentials? userToDisable = context.Credentials.Where(c => c.Username == username).FirstOrDefault();
                    if (userToDisable != null)
                    {
                        userToDisable.IsDisabled = true;
                        context.SaveChanges();
                        return Results.Ok("User disabled successfully");
                    }
                    else
                    {
                        return Results.Problem(title: "User Not Found", statusCode: statusCode.UserNotFound, detail: "The user was not found.");
                    }
                }
                else
                {
                    return Results.Problem(title: "Forbidden", statusCode: statusCode.Forbidden, detail: "You don't have permission to perform this action.");
                }
            }
        }
        else
        {
            return Results.Problem(title: "Token Not Found", statusCode: statusCode.InvalidToken, detail: "The token is invalid. Please authenticate again.");
        }
    }
    catch (System.Exception)
    {
        return Results.Problem(title: "Internal Server Error", statusCode: statusCode.InternalServerError, detail: "An error occurred while processing your request. Please try again later.");
    }
});

/*
    Endpoint - /enableUser
    Method - GET
    Parameters - username, token
    Description - Enables a user
    ReturnCodes -
        200 OK => User enabled successfully
        1000 InternalServerError => An error occurred while processing your request. Please try again later.
        2003 ExpiredToken => Token has expired. Please authenticate again.
        2004 InvalidToken => The token is invalid. Please authenticate again.
        3000 Forbidden => You don't have permission to perform this action.
        4000 UserNotFound => The user was not found.
        4002 UserDisabled => User account is disabled. Please contact the administrator for further assistance.
    Status - Tested OK
*/
app.MapGet("/enableUser", (CredentialContext context, string username, string token) =>
{
    try
    {
        Credentials? user = context.Credentials.Where(c => c.Token == token).FirstOrDefault();
        if (user != null)
        {
            if (user.IsDisabled == true)
            {
                return Results.Problem(title: "User Disabled", statusCode: statusCode.UserDisabled, detail: "User is disabled. Please contact the administrator for further assistance.");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Results.Problem(title: "Token Expired", statusCode: statusCode.ExpiredToken, detail: "Token has expired. Please authenticate again.");
            }
            else
            {
                if (user.IsAdmin == true)
                {
                    Credentials? userToEnable = context.Credentials.Where(c => c.Username == username).FirstOrDefault();
                    if (userToEnable != null)
                    {
                        userToEnable.IsDisabled = false;
                        context.SaveChanges();
                        return Results.Ok("User enabled successfully");
                    }
                    else
                    {
                        return Results.Problem(title: "User Not Found", statusCode: statusCode.UserNotFound, detail: "The user was not found.");
                    }
                }
                else
                {
                    return Results.Problem(title: "Forbidden", statusCode: statusCode.Forbidden, detail: "You don't have permission to perform this action.");
                }
            }
        }
        else
        {
            return Results.Problem(title: "Token Not Found", statusCode: statusCode.InvalidToken, detail: "The token is invalid. Please authenticate again.");
        }
    }
    catch (System.Exception)
    {
        return Results.Problem(title: "Internal Server Error", statusCode: statusCode.InternalServerError, detail: "An error occurred while processing your request. Please try again later.");
    }
});

/*
    Endpoint - /deleteUser
    Method - GET
    Parameters - username, token
    Description - Deletes a user
    ReturnCodes -
        200 OK => User deleted successfully
        1000 InternalServerError => An error occurred while processing your request. Please try again later.
        2003 ExpiredToken => Token has expired. Please authenticate again.
        2004 InvalidToken => The token is invalid. Please authenticate again.
        3000 Forbidden => You don't have permission to perform this action.
        4000 UserNotFound => The user was not found.
        4002 UserDisabled => User account is disabled. Please contact the administrator for further assistance.

*/
app.MapGet("/deleteUser", (CredentialContext context, string username, string token) =>
{
    try
    {
        Credentials? user = context.Credentials.Where(c => c.Token == token).FirstOrDefault();
        if (user != null)
        {
            if (user.IsDisabled == true)
            {
                return Results.Problem(title: "User Disabled", statusCode: statusCode.UserDisabled, detail: "User is disabled. Please contact the administrator for further assistance.");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Results.Problem(title: "Token Expired", statusCode: statusCode.ExpiredToken, detail: "Token has expired. Please authenticate again.");
            }
            else
            {
                if (user.IsAdmin == true)
                {
                    Credentials? userToDelete = context.Credentials.Where(c => c.Username == username).FirstOrDefault();
                    if (userToDelete != null)
                    {
                        context.Credentials.Remove(userToDelete);
                        context.SaveChanges();
                        return Results.Ok("User deleted successfully");
                    }
                    else
                    {
                        return Results.Problem(title: "User Not Found", statusCode: statusCode.UserNotFound, detail: "The user was not found.");
                    }
                }
                else
                {
                    return Results.Problem(title: "Forbidder", statusCode: statusCode.Forbidden, detail: "You don't have permission to perform this action.");
                }
            }
        }
        else
        {
            return Results.Problem(title: "Token Not Found", statusCode: statusCode.InvalidToken, detail: "The token is invalid. Please authenticate again.");
        }
    }
    catch (System.Exception)
    {
        return Results.Problem(title: "Internal Server Error", statusCode: statusCode.InternalServerError, detail: "An error occurred while processing your request. Please try again later.");
    }
});

/*
    Endpoint - /changePassword
    Method - GET
    Parameters - username, oldPassword, newPassword, token
    Description - Changes a user's password
    ReturnCodes -
        200 OK => Password changed successfully
        1000 InternalServerError => An error occurred while processing your request. Please try again later.
        2003 ExpiredToken => Token has expired. Please authenticate again.
        2004 InvalidToken => The token is invalid. Please authenticate again.
        2001 InvalidCredentials => You are not authorized to perform this action.
        4002 UserDisabled => User account is disabled. Please contact the administrator for further assistance.
*/
app.MapGet("/changePassword", (CredentialContext context, string username, string oldPassword, string newPassword, string token) =>
{
    try
    {
        Credentials? user = context.Credentials.Where(c => c.Token == token).FirstOrDefault();
        if (user != null)
        {
            if (user.IsDisabled == true)
            {
                return Results.Problem(title: "User Disabled", statusCode: statusCode.UserDisabled, detail: "User is disabled. Please contact the administrator for further assistance.");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Results.Problem(title: "Token Expired", statusCode: statusCode.ExpiredToken, detail: "Token has expired. Please authenticate again.");
            }
            else
            {
                if (user.Username == username && user.Password == oldPassword)
                {
                    user.Password = newPassword;
                    context.SaveChanges();
                    return Results.Ok("Password changed successfully");
                }
                else
                {
                    return Results.Problem(title: "InvalidCredentials", statusCode: statusCode.InvalidCredentials, detail: "You are not authorized to perform this action.");
                }
            }
        }
        else
        {
            return Results.Problem(title: "Token Not Found", statusCode: statusCode.InvalidToken, detail: "The token is invalid. Please authenticate again.");
        }
    }
    catch (System.Exception)
    {
        return Results.Problem(title: "Internal Server Error", statusCode: statusCode.InternalServerError, detail: "An error occurred while processing your request. Please try again later.");
    }
});

/*
    Endpoint - /resetPassword
    Method - GET
    Parameters - username, newPassword, token
    Description - Resets a user's password
    ReturnCodes -
        200 OK => Password reset successfully
        1000 InternalServerError => An error occurred while processing your request. Please try again later.
        2003 ExpiredToken => Token has expired. Please authenticate again.
        2004 InvalidToken => The token is invalid. Please authenticate again.
        4000 UserNotFound => The user was not found.
        3000 Forbidden => You don't have permission to perform this action. 
        4002 UserDisabled => User account is disabled. Please contact the administrator for further assistance.
*/
app.MapGet("/resetPassword", (CredentialContext context, string username, string newPassword, string token) =>
{
    try
    {
        Credentials? user = context.Credentials.Where(c => c.Token == token).FirstOrDefault();
        if (user != null)
        {
            if (user.IsDisabled == true)
            {
                return Results.Problem(title: "User Disabled", statusCode: statusCode.UserDisabled, detail: "User is disabled. Please contact the administrator for further assistance.");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Results.Problem(title: "Token Expired", statusCode: statusCode.ExpiredToken, detail: "Token has expired. Please authenticate again.");
            }
            else
            {
                if (user.IsAdmin == true)
                {
                    Credentials? userToReset = context.Credentials.Where(c => c.Username == username).FirstOrDefault();
                    if (userToReset != null)
                    {
                        userToReset.Password = newPassword;
                        context.SaveChanges();
                        return Results.Ok("Password reset successfully");
                    }
                    else
                    {
                        return Results.Problem(title: "User Not Found", statusCode: statusCode.UserNotFound, detail: "The user was not found.");
                    }
                }
                else
                {
                    return Results.Problem(title: "Unauthorized", statusCode: statusCode.Forbidden, detail: "You don't have permission to perform this action.");
                }
            }
        }
        else
        {
            return Results.Problem(title: "Token Not Found", statusCode: statusCode.InvalidToken, detail: "The token is invalid. Please authenticate again.");
        }
    }
    catch (System.Exception)
    {
        return Results.Problem(title: "Internal Server Error", statusCode: statusCode.InternalServerError, detail: "An error occurred while processing your request. Please try again later.");
    }
});

/*
    Endpoint - /getuserInfo
    Method - GET
    Parameters - token
    Description - Returns user information
    ReturnCodes -
        200 OK => User information
        1000 InternalServerError => An error occurred while processing your request. Please try again later.
        2003 ExpiredToken => Token has expired. Please authenticate again.
        2004 InvalidToken => The token is invalid. Please authenticate again.
        4000 UserNotFound => The user was not found.
        4002 UserDisabled => User account is disabled. Please contact the administrator for further assistance.
*/
app.MapGet("/getuserInfo", (CredentialContext context, string token) =>
{
    try
    {
        Credentials? user = context.Credentials.Where(c => c.Token == token).FirstOrDefault();
        if (user != null)
        {
            if (user.IsDisabled == true)
            {
                return Results.Problem(title: "User Disabled", statusCode: statusCode.UserDisabled, detail: "User is disabled. Please contact the administrator for further assistance.");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Results.Problem(title: "Token Expired", statusCode: statusCode.ExpiredToken, detail: "Token has expired. Please authenticate again.");
            }
            else
            {
                UserInfo? userInfo = context.UserInfo.Where(u => u.UserId == user.UserId).FirstOrDefault();
                if (userInfo != null)
                {
                    return Results.Ok(userInfo);
                }
                else
                {
                    return Results.Problem(title: "User Not Found", statusCode: statusCode.UserNotFound, detail: "The user was not found.");
                }
            }
        }
        else
        {
            return Results.Problem(title: "Token Not Found", statusCode: statusCode.InvalidToken, detail: "The token is invalid. Please authenticate again.");
        }
    }
    catch (System.Exception)
    {
        return Results.Problem(title: "Internal Server Error", statusCode: statusCode.InternalServerError, detail: "An error occurred while processing your request. Please try again later.");
    }
});

/*
    Endpoint - /updateInfo
    Method - GET
    Parameters - token, firstName, lastName, email, phoneNumber
    Description - Updates user information
    ReturnCodes -
        200 OK => User information updated successfully
        1000 InternalServerError => An error occurred while processing your request. Please try again later.
        2003 ExpiredToken => Token has expired. Please authenticate again.
        2004 InvalidToken => The token is invalid. Please authenticate again.
        4000 UserNotFound => The user was not found.
        4002 UserDisabled => User account is disabled. Please contact the administrator for further assistance.
*/
app.MapGet("/updateInfo", (CredentialContext context, string token, string firstName, string lastName, string email, string phoneNumber) =>
{
    try
    {
        Credentials? user = context.Credentials.Where(c => c.Token == token).FirstOrDefault();
        if (user != null)
        {
            if (user.IsDisabled == true)
            {
                return Results.Problem(title: "User Disabled", statusCode: statusCode.UserDisabled, detail: "User is disabled. Please contact the administrator for further assistance.");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Results.Problem(title: "Token Expired", statusCode: statusCode.ExpiredToken, detail: "Token has expired. Please authenticate again.");
            }
            else
            {
                UserInfo? userInfo = context.UserInfo.Where(u => u.UserId == user.UserId).FirstOrDefault();
                if (userInfo != null)
                {
                    userInfo.FirstName = firstName;
                    userInfo.LastName = lastName;
                    userInfo.Email = email;
                    userInfo.PhoneNumber = phoneNumber;
                    context.SaveChanges();
                    return Results.Ok("User information updated successfully");
                }
                else
                {
                    return Results.Problem(title: "User Not Found", statusCode: statusCode.UserNotFound, detail: "The user was not found.");
                }
            }
        }
        else
        {
            return Results.Problem(title: "Token Not Found", statusCode: statusCode.InvalidToken, detail: "The token is invalid. Please authenticate again.");
        }
    }
    catch (System.Exception)
    {
        return Results.Problem(title: "Internal Server Error", statusCode: statusCode.InternalServerError, detail: "An error occurred while processing your request. Please try again later.");
    }
});

app.Run();