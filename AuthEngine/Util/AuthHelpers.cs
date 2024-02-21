using AuthEngine.Models;

namespace AuthEngine.Util
{
    public static class AuthHelpers
    {
        public static bool checkDisabled(Credentials user)
        {
            if (user.IsDisabled == true)
            {
                return true;
            }
            return false;
        }
    }
}