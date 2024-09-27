using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using RapIni;
using RapLog;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace NSProgram
{
    public enum EOptionType { eSpin, eCheck, eString }

    class COption
    {
        public string cur = "0";
        public int min = -50;
        public int max = 50;
        public string bst = "0";
        public string name = string.Empty;
        public EOptionType oType = EOptionType.eSpin;

        public COption(string s)
        {
            name = s;
        }

        public void LoadFromIni(CRapIni ini)
        {
            oType = CMod.StrToType(ini.Read($"option>{name}>type"));
            min = ini.ReadInt($"option>{name}>min", min);
            max = ini.ReadInt($"option>{name}>max", max);
            cur = ini.Read($"option>{name}>cur", ((min + max) / 2).ToString());
            bst = ini.Read($"option>{name}>bst", cur);
            if(oType == EOptionType.eCheck)
            {
                min = 0;
                max = 1;
            }
            if (oType == EOptionType.eString)
            {
                min = 0;
                max = 9;
            }
        }

        public void SaveToIni(CRapIni ini)
        {
            ini.Write($"option>{name}>type", CMod.TypeToStr(oType));
            ini.Write($"option>{name}>min", min);
            ini.Write($"option>{name}>max", max);
            ini.Write($"option>{name}>cur", cur);
            ini.Write($"option>{name}>bst", bst);
        }

        public bool Modify(int sub, int del)
        {
            int val;
            switch (oType)
            {
                case EOptionType.eCheck:
                    if ((del == 1) && (bst != "true"))
                    {
                        cur = "true";
                        return true;
                    }
                    else if ((del == -1) && (bst != "false"))
                    {
                        cur = "false";
                        return true;
                    }
                    break;
                case EOptionType.eString:
                    string s = bst.Substring(sub, 1);
                    if (!int.TryParse(s, out val))
                        val = 5;
                    if ((val == 0) && (del < 0))
                        return false;
                    if ((val == 9) && (del > 0))
                        return false;
                    val += del;
                    if (val < 0)
                        val = 0;
                    if (val > 9)
                        val = 9;
                    string cu = cur;
                    char c = val.ToString()[0];
                    char[] arr = cu.ToCharArray();
                    arr[sub] = c;
                    cur = string.Join("", arr);
                    return true;
                default:
                    val = Convert.ToInt32(bst);
                    if ((val == min) && (del < 0))
                        return false;
                    if ((val == max) && (del > 0))
                        return false;
                    val += del;
                    if (val < min)
                        val = min;
                    if (val > max)
                        val = max;
                    cur = val.ToString();
                    return true;
            }
            return false;
        }

    }

    class COptionList : List<COption>
    {
        public int start = 0;
        readonly static Random rnd = new Random();

        public void Init()
        {
            start = rnd.Next(Length() * 2);
        }

        public void SaveToIni(CRapIni ini)
        {
            foreach (COption option in this)
                option.SaveToIni(ini);
            ini.Write("mod>start", start);
        }

        public void LoadFromIni(CRapIni ini)
        {
            Clear();
            List<string> ol = ini.ReadKeyList("option");
            foreach (string name in ol)
            {
                COption option = new COption(name);
                option.LoadFromIni(ini);
                Add(option);
            }
            Init();
            start = ini.ReadInt("mod>start", start);
        }

        public int Length()
        {
            int l = 0;
            foreach (COption option in this)
                if (option.oType == EOptionType.eString)
                    l += option.cur.Length;
                else l++;
            return l;
        }

        bool GetIndexSub(int i, out int index, out int sub)
        {
            index = 0;
            sub = 0;
            if (Count == 0)
                return false;
            int l = 0;
            i %= Length();
            for (int n = 0; n < Count; n++)
            {
                COption opt = this[n];
                if (opt.oType == EOptionType.eString)
                    l += opt.cur.Length;
                else l++;
                if (l > i)
                {
                    index = n;
                    sub = l - i - 1;
                    break;
                }
            }
            return true;
        }

        public string OptionsCur()
        {
            string mod = string.Empty;
            foreach (COption opt in this)
                mod += $" {opt.name} {opt.cur}";
            return mod;
        }

        public string OptionsBst()
        {
            string mod = string.Empty;
            foreach (COption opt in this)
                mod += $" {opt.name} {opt.bst}";
            return mod;
        }

        public bool Modify(int fail, int del)
        {
            if (Count == 0)
                return false;
            GetIndexSub(start + fail, out int index, out int sub);
            COption opt = this[index];
            if (opt.Modify(sub,del))
            {
                string s = $"fail {fail} delta {del} {opt.name} {opt.bst} >> {opt.cur}";
                Console.WriteLine(s);
                CMod.log.Add(s);
                return true;
            }
            return false;
        }

    }

    internal class CMod
    {
        string dna = string.Empty;
        public double bstScore = 0;
        int fail = -1;
        int success = -1;
        public readonly COptionList optionList = new COptionList();
        readonly static Random rnd = new Random();
        public static readonly CRapLog log = new CRapLog("mod.log");
        readonly CRapIni ini = new CRapIni(@"mod.ini");


        public CMod()
        {
            LoadFromIni();
        }

        public static string TypeToStr(EOptionType ot)
        {
            switch (ot)
            {
                case EOptionType.eCheck:
                    return "check";
                case EOptionType.eString:
                    return "string";
                default:
                    return "spin";
            }
        }

        public static EOptionType StrToType(string s)
        {
            switch (s)
            {
                case "check":
                    return EOptionType.eCheck;
                case "string":
                    return EOptionType.eString;
                default:
                    return EOptionType.eSpin;
            }
        }

        void LoadFromIni()
        {
            ini.Load();
            optionList.LoadFromIni(ini);
            fail = ini.ReadInt("mod>fail");
            success = ini.ReadInt("mod>success", success);
            bstScore = ini.ReadDouble("mod>score");
        }

        void SaveToIni()
        {
            optionList.SaveToIni(ini);
            ini.Write("mod>fail", fail);
            ini.Write("mod>success", success);
            ini.Write("mod>score", bstScore);
            ini.Save();
        }

        public void Reset()
        {
            optionList.Init();
            foreach (COption opt in optionList)
                opt.cur = opt.bst;
            fail = -1;
            success = -1;
            bstScore = 0;
            Console.WriteLine(optionList.OptionsCur());
        }

        bool Modify(int probe = 0)
        {
            if (optionList.Count == 0)
                return false;
            foreach (COption o in optionList)
                o.cur = o.bst;
            SaveToIni();
            int len = optionList.Length();
            int multi = fail / (len * 2);
            int up = (1 << multi) + success;
            if (((fail / len) & 1) == (optionList.start & 1))
                up = -up;
            if (optionList.Modify(fail + 1, up))
            {
                SaveToIni();
                return true;
            }
            fail++;
            success = 0;
            if (++probe < len * 2)
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
                log.Add($"accuracy ({s:N2}){optionList.OptionsCur()}");
            }
            else
            {
                fail++;
                if (success > 0)
                {
                    fail = 0;
                    optionList.Init();
                }
                success = 0;
            }
            return Modify(0);
        }

    }
}
