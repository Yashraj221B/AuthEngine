namespace AuthEngine.Util
{
    public class  statusCode
    {
        // 1000 - 1999: General errors
        public static int InternalServerError = 1000; //Something unexpected happened on the server-side.
        public static int BadRequest = 1001; //The request was invalid or could not be understood by the server.
        public static int UnsupportedFeature = 1002; //The server does not support the requested feature.
        
        
        // 2000 - 2999: Authentication errors
        public static int Unauthorized = 2000; //The user is not authorized to perform the requested action.
        public static int InvalidCredentials = 2001; //Username or password is incorrect.
        public static int DisabledAccount = 2002; //The user's account is disabled and cannot be accessed.
        public static int ExpiredToken = 2003; //The provided authentication token is no longer valid
        public static int InvalidToken = 2004; //The provided authentication token is invalid or tampered with.
        public static int TokenRequired = 2005; //An authentication token is required to access this resource.

        // 3000 - 3999: Authorization errors
        public static int Forbidden = 3000; //The user is not allowed to access the requested resource.
        
        // 4000 - 4999: User errors
        public static int UserNotFound = 4000; //The requested user was not found.
        public static int UserExists = 4001; //The requested user already exists.
        public static int UserDisabled = 4002; //The requested user account is disabled and cannot be accessed.
        public static int UserNotAuthenticated = 4003; //The user is not authenticated and cannot be accessed.
        public static int UserNotAuthorized = 4004; //The user is not authorized to access the requested resource.
        public static int UserNotActive = 4005; //The user is not active and cannot be accessed.

        // 5000 - 5999: Success
        public static int Success = 5000; //The request was successful.
        public static int Completed = 5001; //The requested action was completed successfully.
        public static int Accepted = 5002; //The request was accepted and will be processed.
        public static int NoContent = 5003; //The request was successful but no content was returned.
        public static int Authenticated = 5004; //The user is authenticated and can access the requested resource.
        public static int Authorized = 5005; //The user is authorized to access the requested resource.
        public static int Active = 5006; //The user is active and can be accessed.
    }
}