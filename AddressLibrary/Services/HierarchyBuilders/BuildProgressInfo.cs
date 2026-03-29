namespace AddressLibrary.Services.HierarchyBuilders
{
    public class BuildProgressInfo
    {
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; }
        public string CurrentOperation { get; set; }
        public double PercentageComplete => TotalSteps > 0 ? (double)CurrentStep / TotalSteps * 100 : 0;

        public BuildProgressInfo(int currentStep, int totalSteps, string currentOperation)
        {
            CurrentStep = currentStep;
            TotalSteps = totalSteps;
            CurrentOperation = currentOperation;
        }
    }
}