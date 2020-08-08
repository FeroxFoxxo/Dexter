namespace Dexter.ConsoleApp {
    public static class Configuration {
        public static string BOT_NAME = "Dexter";

        public static string HEADER = "\n" +
            " ██████╗ ███████╗██╗  ██╗████████╗███████╗██████╗ \n" +
            " ██╔══██╗██╔════╝╚██╗██╔╝╚══██╔══╝██╔════╝██╔══██╗\n" +
            " ██║  ██║█████╗   ╚███╔╝    ██║   █████╗  ██████╔╝\n" +
            " ██║  ██║██╔══╝   ██╔██╗    ██║   ██╔══╝  ██╔══██╗\n" +
            " ██████╔╝███████╗██╔╝ ██╗   ██║   ███████╗██║  ██║\n" +
            " ╚═════╝ ╚══════╝╚═╝  ╚═╝   ╚═╝   ╚══════╝╚═╝  ╚═╝";

        public static string PRESS_KEY = "\n Press any key to continue...";

        public static string MENU_OPTIONS =
            " [1] Edit Bot Token\n" +
            " [2] Start/Stop Dexter\n" +
            " [3] Exit Dexter\n";

        public static string ENTER_NUMBER = " Please select an action by typing its number: ";

        public static string NOT_A_NUMBER = " Please enter a valid number. Don't type in the [ and ] characters. Just the number.";

        public static string ENTER_TOKEN = " Please enter your Discord bot's token: ";

        public static string IS_TOKEN_CORRECT = " You entered the following token: ";

        public static string YES_NO_PROMPT = " Is this token correct? [Y] or [N] ";

        public static string FAILED_TOKEN = "\n\n Failed to apply token! Incorrect token given.";

        public static string CORRECT_TOKEN = "\n\n Applied token!";

        public static string INVALID_CHOICE = " Your choice was not recognized as an option in the menu. Please try again.\n";
    }
}
