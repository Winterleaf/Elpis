namespace Elpis
{
    public static class JumpListManager
    {
        public static System.Windows.Shell.JumpTask createJumpTask(string title, string description, string commandArg,
            int iconIndex)
        {
            System.Windows.Shell.JumpTask task = new System.Windows.Shell.JumpTask
            {
                Title = title,
                Description = description,
                ApplicationPath = System.Reflection.Assembly.GetEntryAssembly().Location,
                Arguments = commandArg
            };
            task.IconResourcePath = task.ApplicationPath;
            task.IconResourceIndex = iconIndex;
            return task;
        }

        public static System.Windows.Shell.JumpTask createJumpTask(System.Windows.Input.RoutedUICommand command,
            string commandArg, int iconIndex)
        {
            return createJumpTask(command.Name, command.Text, commandArg, iconIndex);
        }
    }
}