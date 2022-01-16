namespace ErtisAuth.Hub.Helpers
{
    public static class SvgIcons
    {
        #region Fields & Properties

        private static string asideToggle;
        public static string AsideToggle =>
            asideToggle ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
                    <path d=""M11.2657 11.4343L15.45 7.25C15.8642 6.83579 15.8642 6.16421 15.45 5.75C15.0358 5.33579 14.3642 5.33579 13.95 5.75L8.40712 11.2929C8.01659 11.6834 8.01659 12.3166 8.40712 12.7071L13.95 18.25C14.3642 18.6642 15.0358 18.6642 15.45 18.25C15.8642 17.8358 15.8642 17.1642 15.45 16.75L11.2657 12.5657C10.9533 12.2533 10.9533 11.7467 11.2657 11.4343Z"" fill=""black""/>
                </svg>";
        
        private static string dashboard;
        public static string Dashboard =>
            dashboard ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
                    <rect x=""2"" y=""2"" width=""9"" height=""9"" rx=""2"" fill=""black"" />
                    <rect opacity=""0.3"" x=""13"" y=""2"" width=""9"" height=""9"" rx=""2"" fill=""black"" />
                    <rect opacity=""0.3"" x=""13"" y=""13"" width=""9"" height=""9"" rx=""2"" fill=""black"" />
                    <rect opacity=""0.3"" x=""2"" y=""13"" width=""9"" height=""9"" rx=""2"" fill=""black"" />
                </svg>";

        private static string users;
        public static string Users =>
            users ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
					<path d=""M6.28548 15.0861C7.34369 13.1814 9.35142 12 11.5304 12H12.4696C14.6486 12 16.6563 13.1814 17.7145 15.0861L19.3493 18.0287C20.0899 19.3618 19.1259 21 17.601 21H6.39903C4.87406 21 3.91012 19.3618 4.65071 18.0287L6.28548 15.0861Z"" fill=""black""></path>
					<rect opacity=""0.3"" x=""8"" y=""3"" width=""8"" height=""8"" rx=""4"" fill=""black""></rect>
				</svg>";
        
        private static string roles;
        public static string Roles =>
            roles ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
					<path opacity=""0.3"" d=""M20.5543 4.37824L12.1798 2.02473C12.0626 1.99176 11.9376 1.99176 11.8203 2.02473L3.44572 4.37824C3.18118 4.45258 3 4.6807 3 4.93945V13.569C3 14.6914 3.48509 15.8404 4.4417 16.984C5.17231 17.8575 6.18314 18.7345 7.446 19.5909C9.56752 21.0295 11.6566 21.912 11.7445 21.9488C11.8258 21.9829 11.9129 22 12.0001 22C12.0872 22 12.1744 21.983 12.2557 21.9488C12.3435 21.912 14.4326 21.0295 16.5541 19.5909C17.8169 18.7345 18.8277 17.8575 19.5584 16.984C20.515 15.8404 21 14.6914 21 13.569V4.93945C21 4.6807 20.8189 4.45258 20.5543 4.37824Z"" fill=""black""></path>
					<path d=""M14.854 11.321C14.7568 11.2282 14.6388 11.1818 14.4998 11.1818H14.3333V10.2272C14.3333 9.61741 14.1041 9.09378 13.6458 8.65628C13.1875 8.21876 12.639 8 12 8C11.361 8 10.8124 8.21876 10.3541 8.65626C9.89574 9.09378 9.66663 9.61739 9.66663 10.2272V11.1818H9.49999C9.36115 11.1818 9.24306 11.2282 9.14583 11.321C9.0486 11.4138 9 11.5265 9 11.6591V14.5227C9 14.6553 9.04862 14.768 9.14583 14.8609C9.24306 14.9536 9.36115 15 9.49999 15H14.5C14.6389 15 14.7569 14.9536 14.8542 14.8609C14.9513 14.768 15 14.6553 15 14.5227V11.6591C15.0001 11.5265 14.9513 11.4138 14.854 11.321ZM13.3333 11.1818H10.6666V10.2272C10.6666 9.87594 10.7969 9.57597 11.0573 9.32743C11.3177 9.07886 11.6319 8.9546 12 8.9546C12.3681 8.9546 12.6823 9.07884 12.9427 9.32743C13.2031 9.57595 13.3333 9.87594 13.3333 10.2272V11.1818Z"" fill=""black""></path>
				</svg>";
        
        private static string applications;
        public static string Applications =>
            applications ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
                    <path d=""M2 16C2 16.6 2.4 17 3 17H21C21.6 17 22 16.6 22 16V15H2V16Z"" fill=""black""/>
                    <path opacity=""0.3"" d=""M21 3H3C2.4 3 2 3.4 2 4V15H22V4C22 3.4 21.6 3 21 3Z"" fill=""black""/>
                    <path opacity=""0.3"" d=""M15 17H9V20H15V17Z"" fill=""black""/>
                </svg>";
        
        private static string memberships;
        public static string Memberships =>
            memberships ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
                    <path opacity=""0.3"" d=""M7 20.5L2 17.6V11.8L7 8.90002L12 11.8V17.6L7 20.5ZM21 20.8V18.5L19 17.3L17 18.5V20.8L19 22L21 20.8Z"" fill=""black""/>
                    <path d=""M22 14.1V6L15 2L8 6V14.1L15 18.2L22 14.1Z"" fill=""black""/>
                </svg>";
        
        private static string sessions;
        public static string Sessions =>
            sessions ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
                    <path d=""M16.0173 9H15.3945C14.2833 9 13.263 9.61425 12.7431 10.5963L12.154 11.7091C12.0645 11.8781 12.1072 12.0868 12.2559 12.2071L12.6402 12.5183C13.2631 13.0225 13.7556 13.6691 14.0764 14.4035L14.2321 14.7601C14.2957 14.9058 14.4396 15 14.5987 15H18.6747C19.7297 15 20.4057 13.8774 19.912 12.945L18.6686 10.5963C18.1487 9.61425 17.1285 9 16.0173 9Z"" fill=""black""/>
                    <rect opacity=""0.3"" x=""14"" y=""4"" width=""4"" height=""4"" rx=""2"" fill=""black""/>
                    <path d=""M4.65486 14.8559C5.40389 13.1224 7.11161 12 9 12C10.8884 12 12.5961 13.1224 13.3451 14.8559L14.793 18.2067C15.3636 19.5271 14.3955 21 12.9571 21H5.04292C3.60453 21 2.63644 19.5271 3.20698 18.2067L4.65486 14.8559Z"" fill=""black""/>
                    <rect opacity=""0.3"" x=""6"" y=""5"" width=""6"" height=""6"" rx=""3"" fill=""black""/>
                </svg>";

        private static string providers;
        public static string Providers =>
            providers ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
                    <path opacity=""0.3"" d=""M5 15C3.3 15 2 13.7 2 12C2 10.3 3.3 9 5 9H5.10001C5.00001 8.7 5 8.3 5 8C5 5.2 7.2 3 10 3C11.9 3 13.5 4 14.3 5.5C14.8 5.2 15.4 5 16 5C17.7 5 19 6.3 19 8C19 8.4 18.9 8.7 18.8 9C18.9 9 18.9 9 19 9C20.7 9 22 10.3 22 12C22 13.7 20.7 15 19 15H5ZM5 12.6H13L9.7 9.29999C9.3 8.89999 8.7 8.89999 8.3 9.29999L5 12.6Z"" fill=""black""/>
                    <path d=""M17 17.4V12C17 11.4 16.6 11 16 11C15.4 11 15 11.4 15 12V17.4H17Z"" fill=""black""/>
                    <path opacity=""0.3"" d=""M12 17.4H20L16.7 20.7C16.3 21.1 15.7 21.1 15.3 20.7L12 17.4Z"" fill=""black""/>
                    <path d=""M8 12.6V18C8 18.6 8.4 19 9 19C9.6 19 10 18.6 10 18V12.6H8Z"" fill=""black""/>
                </svg>";
        
        private static string webhooks;
        public static string Webhooks =>
            webhooks ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
                    <path d=""M10.6 10.7C13.3 8 16.9 6.3 20.9 6C21.5 6 21.9 5.5 21.9 4.9V3C21.9 2.4 21.4 2 20.9 2C15.8 2.3 11.2 4.4 7.79999 7.8C4.39999 11.2 2.2 15.8 2 20.9C2 21.5 2.4 21.9 3 21.9H4.89999C5.49999 21.9 6 21.5 6 20.9C6.2 17 7.90001 13.4 10.6 10.7Z"" fill=""black""/>
                    <path opacity=""0.3"" d=""M14.8 14.9C16.4 13.3 18.5 12.2 20.9 12C21.5 11.9 21.9 11.5 21.9 10.9V9C21.9 8.4 21.4 8 20.8 8C17.4 8.3 14.3 9.8 12 12.1C9.7 14.4 8.19999 17.5 7.89999 20.9C7.89999 21.5 8.29999 22 8.89999 22H10.8C11.4 22 11.8 21.6 11.9 21C12.2 18.6 13.2 16.5 14.8 14.9ZM16.2 16.3C17.4 15.1 19 14.3 20.7 14C21.3 13.9 21.8 14.4 21.8 15V17C21.8 17.5 21.4 18 20.9 18.1C20.1 18.3 19.5 18.6 19 19.2C18.5 19.8 18.1 20.4 17.9 21.1C17.8 21.6 17.4 22 16.8 22H14.8C14.2 22 13.7 21.5 13.8 20.9C14.2 19.1 15 17.5 16.2 16.3Z"" fill=""black""/>
                </svg>";
        
        private static string events;
        public static string Events =>
            events ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
                    <path opacity=""0.3"" d=""M21.25 18.525L13.05 21.825C12.35 22.125 11.65 22.125 10.95 21.825L2.75 18.525C1.75 18.125 1.75 16.725 2.75 16.325L4.04999 15.825L10.25 18.325C10.85 18.525 11.45 18.625 12.05 18.625C12.65 18.625 13.25 18.525 13.85 18.325L20.05 15.825L21.35 16.325C22.35 16.725 22.35 18.125 21.25 18.525ZM13.05 16.425L21.25 13.125C22.25 12.725 22.25 11.325 21.25 10.925L13.05 7.62502C12.35 7.32502 11.65 7.32502 10.95 7.62502L2.75 10.925C1.75 11.325 1.75 12.725 2.75 13.125L10.95 16.425C11.65 16.725 12.45 16.725 13.05 16.425Z"" fill=""black""/>
                    <path d=""M11.05 11.025L2.84998 7.725C1.84998 7.325 1.84998 5.925 2.84998 5.525L11.05 2.225C11.75 1.925 12.45 1.925 13.15 2.225L21.35 5.525C22.35 5.925 22.35 7.325 21.35 7.725L13.05 11.025C12.45 11.325 11.65 11.325 11.05 11.025Z"" fill=""black""/>
                </svg>";
        
        private static string fingerprint;
        public static string Fingerprint =>
            fingerprint ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
					<path opacity=""0.3"" d=""M21 10.7192H3C2.4 10.7192 2 11.1192 2 11.7192C2 12.3192 2.4 12.7192 3 12.7192H6V14.7192C6 18.0192 8.7 20.7192 12 20.7192C15.3 20.7192 18 18.0192 18 14.7192V12.7192H21C21.6 12.7192 22 12.3192 22 11.7192C22 11.1192 21.6 10.7192 21 10.7192Z"" fill=""black""></path>
					<path d=""M11.6 21.9192C11.4 21.9192 11.2 21.8192 11 21.7192C10.6 21.4192 10.5 20.7191 10.8 20.3191C11.7 19.1191 12.3 17.8191 12.7 16.3191C12.8 15.8191 13.4 15.4192 13.9 15.6192C14.4 15.7192 14.8 16.3191 14.6 16.8191C14.2 18.5191 13.4 20.1192 12.4 21.5192C12.2 21.7192 11.9 21.9192 11.6 21.9192ZM8.7 19.7192C10.2 18.1192 11 15.9192 11 13.7192V8.71917C11 8.11917 11.4 7.71917 12 7.71917C12.6 7.71917 13 8.11917 13 8.71917V13.0192C13 13.6192 13.4 14.0192 14 14.0192C14.6 14.0192 15 13.6192 15 13.0192V8.71917C15 7.01917 13.7 5.71917 12 5.71917C10.3 5.71917 9 7.01917 9 8.71917V13.7192C9 15.4192 8.4 17.1191 7.2 18.3191C6.8 18.7191 6.9 19.3192 7.3 19.7192C7.5 19.9192 7.7 20.0192 8 20.0192C8.3 20.0192 8.5 19.9192 8.7 19.7192ZM6 16.7192C6.5 16.7192 7 16.2192 7 15.7192V8.71917C7 8.11917 7.1 7.51918 7.3 6.91918C7.5 6.41918 7.2 5.8192 6.7 5.6192C6.2 5.4192 5.59999 5.71917 5.39999 6.21917C5.09999 7.01917 5 7.81917 5 8.71917V15.7192V15.8191C5 16.3191 5.5 16.7192 6 16.7192ZM9 4.71917C9.5 4.31917 10.1 4.11918 10.7 3.91918C11.2 3.81918 11.5 3.21917 11.4 2.71917C11.3 2.21917 10.7 1.91916 10.2 2.01916C9.4 2.21916 8.59999 2.6192 7.89999 3.1192C7.49999 3.4192 7.4 4.11916 7.7 4.51916C7.9 4.81916 8.2 4.91918 8.5 4.91918C8.6 4.91918 8.8 4.81917 9 4.71917ZM18.2 18.9192C18.7 17.2192 19 15.5192 19 13.7192V8.71917C19 5.71917 17.1 3.1192 14.3 2.1192C13.8 1.9192 13.2 2.21917 13 2.71917C12.8 3.21917 13.1 3.81916 13.6 4.01916C15.6 4.71916 17 6.61917 17 8.71917V13.7192C17 15.3192 16.8 16.8191 16.3 18.3191C16.1 18.8191 16.4 19.4192 16.9 19.6192C17 19.6192 17.1 19.6192 17.2 19.6192C17.7 19.6192 18 19.3192 18.2 18.9192Z"" fill=""black""></path>
				</svg>";
        
        private static string contents;
        public static string Contents =>
            contents ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
                    <path opacity=""0.3"" d=""M7.16973 20.95C6.26973 21.55 5.16972 20.75 5.46972 19.75L7.36973 14.05L2.46972 10.55C1.56972 9.95005 2.06973 8.55005 3.06973 8.55005H20.8697C21.9697 8.55005 22.3697 9.95005 21.4697 10.55L7.16973 20.95Z"" fill=""black""/>
                    <path d=""M11.0697 2.75L7.46973 13.95L16.9697 20.85C17.8697 21.45 18.9697 20.65 18.6697 19.65L13.1697 2.75C12.7697 1.75 11.3697 1.75 11.0697 2.75Z"" fill=""black""/>
                </svg>";
        
        private static string contentTypes;
        public static string ContentTypes =>
            contentTypes ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""25"" viewBox=""0 0 24 25"" fill=""none"">
                    <path opacity=""0.3"" d=""M8.9 21L7.19999 22.6999C6.79999 23.0999 6.2 23.0999 5.8 22.6999L4.1 21H8.9ZM4 16.0999L2.3 17.8C1.9 18.2 1.9 18.7999 2.3 19.1999L4 20.9V16.0999ZM19.3 9.1999L15.8 5.6999C15.4 5.2999 14.8 5.2999 14.4 5.6999L9 11.0999V21L19.3 10.6999C19.7 10.2999 19.7 9.5999 19.3 9.1999Z"" fill=""black""/>
                    <path d=""M21 15V20C21 20.6 20.6 21 20 21H11.8L18.8 14H20C20.6 14 21 14.4 21 15ZM10 21V4C10 3.4 9.6 3 9 3H4C3.4 3 3 3.4 3 4V21C3 21.6 3.4 22 4 22H9C9.6 22 10 21.6 10 21ZM7.5 18.5C7.5 19.1 7.1 19.5 6.5 19.5C5.9 19.5 5.5 19.1 5.5 18.5C5.5 17.9 5.9 17.5 6.5 17.5C7.1 17.5 7.5 17.9 7.5 18.5Z"" fill=""black""/>
                </svg>";

        private static string timesSquare;
        public static string TimesSquare =>
            timesSquare ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
                    <rect opacity=""0.3"" x=""2"" y=""2"" width=""20"" height=""20"" rx=""5"" fill=""black""/>
                    <rect x=""7"" y=""15.3137"" width=""12"" height=""2"" rx=""1"" transform=""rotate(-45 7 15.3137)"" fill=""black""/>
                    <rect x=""8.41422"" y=""7"" width=""12"" height=""2"" rx=""1"" transform=""rotate(45 8.41422 7)"" fill=""black""/>
                </svg>";
        
        private static string search;
        public static string Search =>
            search ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
					<rect opacity=""0.5"" x=""17.0365"" y=""15.1223"" width=""8.15546"" height=""2"" rx=""1"" transform=""rotate(45 17.0365 15.1223)"" fill=""black"" />
					<path d=""M11 19C6.55556 19 3 15.4444 3 11C3 6.55556 6.55556 3 11 3C15.4444 3 19 6.55556 19 11C19 15.4444 15.4444 19 11 19ZM11 5C7.53333 5 5 7.53333 5 11C5 14.4667 7.53333 17 11 17C14.4667 17 17 14.4667 17 11C17 7.53333 14.4667 5 11 5Z"" fill=""black"" />
				</svg>";
        
        private static string primary;
        public static string Primary =>
            primary ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""16"" height=""16"" fill=""currentColor"" class=""bi bi-shield-check"" viewBox=""0 0 16 16"">
                    <path d=""M5.338 1.59a61.44 61.44 0 0 0-2.837.856.481.481 0 0 0-.328.39c-.554 4.157.726 7.19 2.253 9.188a10.725 10.725 0 0 0 2.287 2.233c.346.244.652.42.893.533.12.057.218.095.293.118a.55.55 0 0 0 .101.025.615.615 0 0 0 .1-.025c.076-.023.174-.061.294-.118.24-.113.547-.29.893-.533a10.726 10.726 0 0 0 2.287-2.233c1.527-1.997 2.807-5.031 2.253-9.188a.48.48 0 0 0-.328-.39c-.651-.213-1.75-.56-2.837-.855C9.552 1.29 8.531 1.067 8 1.067c-.53 0-1.552.223-2.662.524zM5.072.56C6.157.265 7.31 0 8 0s1.843.265 2.928.56c1.11.3 2.229.655 2.887.87a1.54 1.54 0 0 1 1.044 1.262c.596 4.477-.787 7.795-2.465 9.99a11.775 11.775 0 0 1-2.517 2.453 7.159 7.159 0 0 1-1.048.625c-.28.132-.581.24-.829.24s-.548-.108-.829-.24a7.158 7.158 0 0 1-1.048-.625 11.777 11.777 0 0 1-2.517-2.453C1.928 10.487.545 7.169 1.141 2.692A1.54 1.54 0 0 1 2.185 1.43 62.456 62.456 0 0 1 5.072.56z""/>
                    <path d=""M10.854 5.146a.5.5 0 0 1 0 .708l-3 3a.5.5 0 0 1-.708 0l-1.5-1.5a.5.5 0 1 1 .708-.708L7.5 7.793l2.646-2.647a.5.5 0 0 1 .708 0z""/>
                </svg>";
        
        private static string success;
        public static string Success =>
            success ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""16"" height=""16"" fill=""currentColor"" class=""bi bi-check2-circle"" viewBox=""0 0 16 16"">
                    <path d=""M2.5 8a5.5 5.5 0 0 1 8.25-4.764.5.5 0 0 0 .5-.866A6.5 6.5 0 1 0 14.5 8a.5.5 0 0 0-1 0 5.5 5.5 0 1 1-11 0z""/>
                    <path d=""M15.354 3.354a.5.5 0 0 0-.708-.708L8 9.293 5.354 6.646a.5.5 0 1 0-.708.708l3 3a.5.5 0 0 0 .708 0l7-7z""/>
                </svg>";
        
        private static string info;
        public static string Info =>
            info ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
                    <rect opacity=""0.3"" x=""2"" y=""2"" width=""20"" height=""20"" rx=""10"" fill=""black""/>
                    <rect x=""11"" y=""17"" width=""7"" height=""2"" rx=""1"" transform=""rotate(-90 11 17)"" fill=""black""/>
                    <rect x=""11"" y=""9"" width=""2"" height=""2"" rx=""1"" transform=""rotate(-90 11 9)"" fill=""black""/>
                </svg>";
        
        private static string warning;
        public static string Warning =>
            warning ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"">
                    <rect opacity=""0.3"" x=""2"" y=""2"" width=""20"" height=""20"" rx=""10"" fill=""black""/>
                    <rect x=""11"" y=""14"" width=""7"" height=""2"" rx=""1"" transform=""rotate(-90 11 14)"" fill=""black""/>
                    <rect x=""11"" y=""17"" width=""2"" height=""2"" rx=""1"" transform=""rotate(-90 11 17)"" fill=""black""/>
                </svg>";
        
        private static string danger;
        public static string Danger =>
            danger ??= @"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""16"" height=""16"" fill=""currentColor"" class=""bi bi-exclamation-triangle-fill"" viewBox=""0 0 16 16"">
                    <path d=""M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5zm.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2z""/>
                </svg>";
        
        #endregion

        #region Methods

        public static string GetByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            
            return name switch
            {
                "aside-toggle" => AsideToggle,
                "dashboard" => Dashboard,
                "users" => Users,
                "roles" => Roles,
                "applications" => Applications,
                "memberships" => Memberships,
                "sessions" => Sessions,
                "providers" => Providers,
                "webhooks" => Webhooks,
                "events" => Events,
                "fingerprint" => Fingerprint,
                "contents" => Contents,
                "content-types" => ContentTypes,
                "search" => Search,
                "primary" => Primary,
                "success" => Success,
                "info" => Info,
                "warning" => Warning,
                "danger" => Danger,
                _ => null
            };
        }

        #endregion
    }
}