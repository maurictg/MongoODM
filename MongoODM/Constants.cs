using System.Text.RegularExpressions;

namespace MongoODM
{
    public static class Constants
    {
        public static Regex ObjectIdRegex = new(@"^[a-f\d]{24}$");
    }
}