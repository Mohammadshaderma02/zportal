namespace ZainEMPProtal.Models
{
    [Serializable]
    public class LoginObject
    {
        public Data Data { get; set; }
        public Logindata LoginData { get; set; }
        public string Msg { get; set; }
        public int Response { get; set; }
        public string EmployeeNT { get; set; }
    }
    [Serializable]
    public class Data
    {
        public string pFNumberField { get; set; }
        public string userTokeField { get; set; }
    }
    [Serializable]
    public class Logindata
    {
        public string emailField { get; set; }
        public string extensionField { get; set; }
        public string fullNameField { get; set; }
        public string isAuthenticatedField { get; set; }
        public string mPFNumberField { get; set; }
        public string managerNameField { get; set; }
        public string mobileNumberField { get; set; }
        public string pFNumberField { get; set; }
        public string positionIDField { get; set; }
    }
}
