using OpenQA.Selenium;
using System.Collections.Generic;

namespace Globals
{
    public static class Constants
    {
        public const string ArchiveBaseUrl = "https://web.archive.org/web";

        public const string LookismAvatarDomain = "lksm.ams3.digitaloceanspaces.com";

        public const int IncelsDomain = 1;
        public const int IncelsNetDomain = 2;
        public const int LooksmaxDomain = 3;
        public const int NeetsDomain = 4;
        public const int NcuDomain = 5;
        public const int BlackpillDomain = 6;
        public const int LookismDomain = 7;
        public const int LooksTheoryDomain = 8;
        public const int SanctionedSuicideDomain = 9;
        public const int OtherDomain = 10;
        public const int DefaultDomain = IncelsDomain;
        public static readonly SortedDictionary<int, string> Domains = new()
        {
            { IncelsDomain, "incels.is" },
            { IncelsNetDomain, "incels.net" },
            { LooksmaxDomain, "looksmax.org" },
            { NeetsDomain, "neets.me" },
            { NcuDomain, "ncu.su" },
            { BlackpillDomain, "blackpill.club" },
            { LookismDomain, "lookism.net" },
            { LooksTheoryDomain, "lookstheory.org" },
            { SanctionedSuicideDomain, "sanctioned-suicide.org" },
            { OtherDomain, "Other." }
        };
        public static readonly SortedDictionary<int, string[]> DomainHistory = new()
        {
            {
                IncelsDomain,
                new string[]
                {
                    "incels.me",
                    "incels.co",
                    Domains[IncelsDomain]
                }
            },
            {
                IncelsNetDomain,
                new string[]
                {
                    Domains[IncelsNetDomain]
                }
            },
            {
                LooksmaxDomain,
                new string[]
                {
                    "looksmax.me",
                    "looksmax.co",
                    "looksm.ax",
                    Domains[LooksmaxDomain]
                }
            },
            {
                NeetsDomain,
                new string[]
                {
                    Domains[NeetsDomain]
                }
            },
            {
                NcuDomain,
                new string[]
                {
                    Domains[NcuDomain]
                }
            },
            {
                BlackpillDomain,
                new string[]
                {
                    "blackpill.su",
                    Domains[BlackpillDomain]
                }
            },
            {
                LookismDomain,
                new string[]
                {
                    Domains[LookismDomain]
                }
            },
            {
                LooksTheoryDomain,
                new string[]
                {
                    "lookstheory.net",
                    Domains[LooksTheoryDomain]
                }
            },
            {
                SanctionedSuicideDomain,
                new string[]
                {
                    "sanctionedsuicide.net",
                    "sanctionedsuicide.com",
                    Domains[SanctionedSuicideDomain]
                }
            },
            {
                OtherDomain,
                new string[]
                {
                    Domains[OtherDomain]
                }
            }
        };

        public static readonly By DefaultRefreshBy = By.ClassName("p-nav");
    }
}
