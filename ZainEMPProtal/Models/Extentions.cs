namespace ZainEMPProtal.Models
{
    public static class Extentions
    {
        public static string GetPF(this string pf)
        {
            if (pf.ToLower().Contains("j") == false)
            {
                if (pf.Length == 2)
                    pf = string.Concat("J0000", pf);
                else if (pf.Length == 3)
                    pf = string.Concat("J000", pf);
                else if (pf.Length == 4)
                    pf = string.Concat("J00", pf);
                else if (pf.Length == 5)
                    pf = string.Concat("J0", pf);
            }
            return pf;
        }
        public static bool isManager(this string job)
        {
            if (job.Trim().ToLower() == "manager"
                || job.Trim().ToLower() == "executive manager"
                || job.Trim().ToLower() == "senior manager"
                || job.Trim().ToLower() == "division leader"
                || job.Trim().ToLower() == "professional"
                || job.Trim().ToLower() == "senior division leader")
                return true;
            return false;
        }
        public static bool isDirector(this string job)
        {
            if (job.Trim().ToLower() == "director" || job.ToLower() == "chief director")
                return true;
            return false;
        }
    }

}
