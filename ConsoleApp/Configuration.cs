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

        public static string MENU_OPTIONS =
            " [1] Edit Bot Token\n" +
            " [2] Start/Stop Dexter\n" +
            " [3] Exit Dexter";

        public static string ENTER_NUMBER = "\n\n Please select an action by typing its number: ";

        public static string NOT_A_NUMBER = "\n\n Please enter a valid number. Don't type in the [ and ] characters. Just the number.";

        public static string ENTER_TOKEN = "\n\n Please enter your Discord bot's token: ";

        public static string IS_TOKEN_CORRECT = "\n You entered the following token: ";

        public static string YES_NO_PROMPT = "\n Is this token correct? [Y] or [N] ";

        public static string FAILED_TOKEN = "\n\n Failed to apply token! Incorrect token given.";

        public static string CORRECT_TOKEN = "\n\n Applied token!";

        public static string INVALID_CHOICE = "\n Your choice was not recognized as an option in the menu. Please try again.";

        public static string START_DEXTER = "Starting Dexter. Please wait...";

        public static string STARTED_DEXTER = BOT_NAME + " has started successfully!";

        public static string STOP_DEXTER = "Stopping Dexter. Please wait...";

        public static string STOPPED_DEXTER = BOT_NAME + " has halted successfully!";
    }
}
