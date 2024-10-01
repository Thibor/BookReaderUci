using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using RapIni;
using RapLog;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace NSProgram
{
    public enum EOptionType { eSpin, eCheck, eString }

    class CHis
    {
        public double score;
        public int index;
        public int delta;

        public string ToStr()
        {
            return $"{score} {index} {delta}";
        }

        public void FromStr(string s)
        {
            string[] a = s.Split();
            score = Convert.ToDouble(a[0]);
            index = Convert.ToInt32(a[1]);
            delta = Convert.ToInt32(a[2]);
        }

    }

    class CHisList : List<CHis>
    {
        int limit = 3;

        public bool AddHis(CHis his)
        {
            if (his.delta == 0)
                return false;
            for (int n = 0; n < Count; n++)
                if (this[n].score < his.score)
                {
                    Insert(n, his);
                    if (Count > limit)
                        RemoveRange(limit, Count - limit);
                    return Count == limit;
                }
            if (Count < limit)
            {
                Add(his);
                return Count == limit;
            }
            return false;
        }

        public CHis Last()
        {
            if (Count > 0)
                return this[Count - 1];
            return null;
        }

        public void FromSl(List<string> sl)
        {
            Clear();
            for (int n = 0; n < sl.Count; n++)
            {
                CHis h = new CHis();
                h.FromStr(sl[n]);
                Add(h);
            }
        }

        public List<string> ToSl()
        {
            List<string> sl = new List<string>();
            for (int n = 0; n < Count; n++)
                sl.Add(this[n].ToStr());
            return sl;
        }

        public bool Add(double score, int index, int delta)
        {
            CHis h = new CHis();
            h.score = score;
            h.index = index;
            h.delta = delta;
            return AddHis(h);
        }

    }

    class CMix : List<int>
    {
        static readonly Random rnd = new Random();

        public void Shuffle()
        {
            for (int n = 0; n < Count; n++)
            {
                int r = rnd.Next(Count);
                (this[r], this[n]) = (this[n], this[r]);
            }
        }

        public void SetLen(int len)
        {
            Clear();
            int v = 0;
            for (int n = 0; n < len; n++)
                for (int m = 0; m < 2; m++)
                    Add(v++);
            Shuffle();
        }

        public void SetList(List<int> list)
        {
            Clear();
            foreach (int n in list)
                Add(n);
        }

        public int GetVal(int i)
        {
            return this[i % Count];
        }

    }

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
            if (oType == EOptionType.eCheck)
            {
                min = 0;
                max = 1;
            }
            min = ini.ReadInt($"option>{name}>min", min);
            max = ini.ReadInt($"option>{name}>max", max);
            cur = ini.Read($"option>{name}>cur", ((min + max) / 2).ToString());
            bst = ini.Read($"option>{name}>bst", cur);
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
                    string[] tokens = bst.Trim().Split();
                    if (!int.TryParse(tokens[sub], out val))
                        val = 0;
                    val += del;
                    if ((val < min) || (val > max))
                        return false;
                    tokens[sub] = val.ToString();
                    cur = string.Join(" ", tokens);
                    return true;
                default:
                    val = Convert.ToInt32(bst);
                    val += del;
                    if ((val < min) || (val > max))
                        return false;
                    cur = val.ToString();
                    return true;
            }
            return false;
        }

    }

    class COptionList : List<COption>
    {
        public int index = 0;
        public int delta = 0;
        public int length = 0;
        public CMix mix = new CMix();
        readonly static Random rnd = new Random();

        public void Init()
        {
            int len = Length();
            mix.SetLen(len);
        }

        public void SaveToIni(CRapIni ini)
        {
            foreach (COption option in this)
                option.SaveToIni(ini);
            ini.Write("mod>mix", mix);
            ini.Write("mod>length", length);
            ini.Write("mod>index", index);
            ini.Write("mod>delta", delta);
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
            index = ini.ReadInt("mod>index");
            delta = ini.ReadInt("mod>delta");
            mix.SetList(ini.ReadListInt("mod>mix"));
            length = Length();
            if (mix.Count != length * 2)
                mix.SetLen(length);
        }

        public int Length()
        {
            int l = 0;
            foreach (COption option in this)
                if (option.oType == EOptionType.eString)
                    l += option.cur.Split().Length;
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
            i %= length;
            for (int n = 0; n < Count; n++)
            {
                COption opt = this[n];
                if (opt.oType == EOptionType.eString)
                    l += opt.cur.Split().Length;
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

        public void BstToCur()
        {
            foreach (COption opt in this)
                opt.cur = opt.bst;
        }

        public void CurToBst()
        {
            foreach (COption opt in this)
                opt.bst = opt.cur;
        }

        public void Modify()
        {
            BstToCur();
            Modify(index, delta);
        }

        public bool Modify(int i, int d)
        {
            i = i % length;
            GetIndexSub(i, out int idx, out int sub);
            return this[idx].Modify(sub, d);
        }

        public bool Modify(double bstScore, int fail, int success)
        {
            if (Count == 0)
                return false;
            int value = mix.GetVal(fail);
            delta = 1 << (fail / (length * 2) + value / (length * 2)) + success;
            if (value % (length * 2) < length)
                delta = -delta;
            index = value % length;
            GetIndexSub(value, out int idx, out int sub);
            COption opt = this[idx];
            if (opt.Modify(sub, delta))
            {
                string s = $"best {bstScore:N2} fail {fail} delta {delta} {opt.name} {opt.bst} >> {opt.cur}";
                Console.WriteLine(s);
                return true;
            }
            return false;
        }

    }

    internal class CMod
    {
        public double bstScore = 0;
        double kilScore = 0;
        int kilFail = 0;
        int fail = 0;
        int success = 0;
        int extra = 0;
        CHisList hl = new CHisList();
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
            extra = ini.ReadInt("mod>extra");
            fail = ini.ReadInt("mod>fail");
            kilFail = ini.ReadInt("mod>kilFail");
            success = ini.ReadInt("mod>success", success);
            bstScore = ini.ReadDouble("mod>score");
            kilScore = ini.ReadDouble("mod>kilScore");
            hl.FromSl(ini.ReadListStr("mod>his", "|"));
        }

        public void SaveToIni()
        {
            optionList.SaveToIni(ini);
            ini.Write("mod>extra", extra);
            ini.Write("mod>fail", fail);
            ini.Write("mod>kilFail", kilFail);
            ini.Write("mod>success", success);
            ini.Write("mod>score", bstScore);
            ini.Write("mod>kilScore", kilScore);
            ini.Write("mod>his", hl.ToSl(), "|");
            ini.Save();
        }

        public void Reset()
        {
            optionList.Init();
            optionList.BstToCur();
            fail = 0;
            success = 0;
            bstScore = 0;
            Console.WriteLine(optionList.OptionsCur());
        }

        bool Modify(int probe = 0)
        {
            if (optionList.Count == 0)
                return false;
            optionList.BstToCur();
            if (optionList.Modify(bstScore, fail, success))
                return true;
            fail++;
            success = 0;
            if (++probe < optionList.mix.Count)
                return Modify(probe);
            return false;
        }

        public bool SetScore(double s)
        {
            bool added = extra == 0 ? hl.Add(s, optionList.index, optionList.delta) : false;
            if (extra > 0)
                extra--;
            if (bstScore < s)
            {
                if (extra == 0)
                    success++;
                if (success == 0)
                    fail = 0;
                extra = 0;
                bstScore = s;
                optionList.CurToBst();
                optionList.Init();
                log.Add($"bst ({s:N2}){optionList.OptionsCur()}");
            }
            else
            {
                kilFail++;
                if (s + kilFail * 0.1 > kilScore)
                {
                    kilScore = s;
                    kilFail = 0;
                    log.Add($"sec ({s:N2}){optionList.OptionsCur()}");
                }
                success = 0;
                if (added && (extra == 0))
                    extra = 2;
                if (extra > 0)
                {
                    optionList.Modify();
                    int modified = 0;
                    for (int n = 0; n < hl.Count; n++)
                        if (hl[n].index != optionList.index)
                        {
                            optionList.Modify(hl[n].index, hl[n].delta);
                            if (++modified >= extra)
                                break;
                        }
                    if (extra > modified)
                        extra = modified;
                    Console.WriteLine($"extra {extra}");
                    return true;
                }
                fail++;
            }
            return Modify(0);
        }

    }
}
