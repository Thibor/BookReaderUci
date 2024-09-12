using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using RapIni;
using RapLog;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace NSProgram
{

    class COption
    {
        public int cur = 0;
        public int min = 0;
        public int max = 0;
        public int bst = 0;
        public string name = string.Empty;

        public COption(string s)
        {
            name = s;
        }

        public void LoadFromIni(CRapIni ini)
        {
            bst = ini.ReadInt($"option>{name}>bst");
            min = ini.ReadInt($"option>{name}>min");
            max = ini.ReadInt($"option>{name}>max");
            cur = ini.ReadInt($"option>{name}>cur", bst);
        }

        public void SaveToIni(CRapIni ini)
        {
            ini.Write($"option>{name}>bst", bst);
            ini.Write($"option>{name}>min", min);
            ini.Write($"option>{name}>max", max);
            ini.Write($"option>{name}>cur", cur);
        }

    }

    internal class CMod
    {
        public double bstScore = 0;
        int start = 0;
        int fail = 0;
        int success = 0;
        public readonly List<COption> optionList = new List<COption>();
        static Random rnd = new Random();
        CRapLog log = new CRapLog("mod.log");
        readonly CRapIni ini = new CRapIni(@"mod.ini");


        public CMod()
        {
            LoadFromIni();
        }

        void LoadFromIni()
        {
            ini.Load();
            List<string> ol = new List<string>();
            ol = ini.ReadKeyList("option");
            optionList.Clear();
            foreach (string o in ol)
            {
                COption option = new COption(o);
                option.LoadFromIni(ini);
                optionList.Add(option);
            }
            start = rnd.Next(optionList.Count * 2);
            start = ini.ReadInt("start", start);
            fail = ini.ReadInt("fail");
            success = ini.ReadInt("success", success);
            bstScore = ini.ReadDouble("score");
        }

        void SaveToIni()
        {
            foreach (COption opt in optionList)
                opt.SaveToIni(ini);
            ini.Write("start", start);
            ini.Write("fail", fail);
            ini.Write("success", success);
            ini.Write("score", bstScore);
            ini.Save();
        }

        public string OptionsCur()
        {
            string mod = string.Empty;
            foreach (COption opt in optionList)
                if ((opt.min == 0) && (opt.max == 1))
                    mod += $" {opt.name} {opt.cur == 1}";
                else
                    mod += $" {opt.name} {opt.cur}";
            return mod;
        }

        bool Modify(int probe = 0)
        {
            if (optionList.Count == 0)
                return false;
            foreach (COption o in optionList)
                o.cur = o.bst;
            SaveToIni();
            int index = (fail + start) % optionList.Count;
            int multi = fail / (optionList.Count * 2);
            int up = (1 << multi) + success;
            if (((fail / optionList.Count) & 1) == (start & 1))
                up = -up;
            COption opt = optionList[index];
            int cur = opt.bst + up;
            if ((cur >= opt.min) && (cur <= opt.max))
            {
                opt.cur = cur;
                SaveToIni();
                return true;
            }
            fail++;
            if (++probe < optionList.Count * 2)
                return Modify(probe);
            return false;
        }

        public bool SetScore(double s)
        {
            if (bstScore < s)
            {
                success++;
                bstScore = s;
                foreach (COption opt in optionList)
                    opt.bst = opt.cur;
                SaveToIni();
                log.Add($"accuracy ({s:N2}){OptionsCur()}");
            }
            else
            {
                fail++;
                if (success > 0)
                {
                    fail = 0;
                    start = rnd.Next(optionList.Count * 2);
                }
                success = 0;
            }
            return Modify(0);
        }

    }
}
