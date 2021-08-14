using OpenQA.Selenium;

namespace Globals
{
    public static class Constants
    {
        public const string DefaultDomain = "is";
        public static readonly string[] Domains = new string[] { "me", "co", DefaultDomain };
        
        public static readonly By DefaultRefreshBy = By.ClassName("p-nav");
    }
}
