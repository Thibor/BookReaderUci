using RapIni;
using RapLog;
using System;
using System.Collections.Generic;

namespace NSProgram
{
    public enum EOptionType { eSpin, eCheck, eString }

    class CLast : List<double>
    {
        public bool AddVal(double v)
        {
            bool bst = true;
            foreach (double d in this)
            {
                if (d > v)
                    bst = false;
            }
            Add(v);
            if (Count > 10)
                RemoveRange(0, Count - 10);
            return bst;
        }

        public void FromSl(List<string> sl)
        {
            Clear();
            for (int n = 0; n < sl.Count; n++)
                Add(Convert.ToDouble(sl[n]));
        }

        public List<string> ToSl()
        {
            List<string> sl = new List<string>();
            for (int n = 0; n < Count; n++)
                sl.Add(this[n].ToString());
            return sl;
        }

        public double Max()
        {
            double max = 0;
            foreach (double v in this)
                if (v > max)
                    max = v;
            return max;
        }

    }

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

        public bool AddHis(CHis his)
        {
            if (his.delta == 0)
                return false;
            for (int n = Count - 1; n >= 0; n--)
            {
                CHis h = this[n];
                if (h.index == his.index)
                    if (h.score >= his.score)
                        return false;
                    else
                        RemoveAt(n);
            }
            for (int n = 0; n < Count; n++)
                if (this[n].score < his.score)
                {
                    Insert(n, his);
                    return true;
                }
            Add(his);
            return false;
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
            CHis h = new CHis
            {
                score = score,
                index = index,
                delta = delta
            };
            return AddHis(h);
        }

        public bool RemoveIndex(int i)
        {
            for (int n = Count - 1; n >= 0; n--)
                if (this[n].index == i)
                {
                    RemoveAt(n);
                    return true;
                }
            return false;
        }

        public double Max()
        {
            double max = 0;
            foreach (CHis h in this)
                if (max < h.score)
                    max = h.score;
            return max;
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
        public bool enabled = false;
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

        string BstToEle()
        {
            string result = string.Empty;
            string[] tokens = bst.Trim().Split();
            for (int i = 0; i < tokens.Length; i++)
                result += $" {i + 1}={tokens[i]}";
            return result.Trim();
        }

        public void LoadFromIni(CRapIni ini)
        {
            enabled = ini.ReadBool($"option>{name}>enabled", true);
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
            ini.Write($"option>{name}>enabled", enabled);
            ini.Write($"option>{name}>type", CMod.TypeToStr(oType));
            ini.Write($"option>{name}>min", min);
            ini.Write($"option>{name}>max", max);
            ini.Write($"option>{name}>cur", cur);
            ini.Write($"option>{name}>bst", bst);
            ini.Write($"option>{name}>ele", BstToEle());
        }

        public bool Modify(int sub, int del)
        {
            int val;
            if (enabled)
                switch (oType)
                {
                    case EOptionType.eCheck:
                        if ((del == 1) && (cur != "true"))
                        {
                            cur = "true";
                            return true;
                        }
                        else if ((del == -1) && (cur != "false"))
                        {
                            cur = "false";
                            return true;
                        }
                        break;
                    case EOptionType.eString:
                        string[] tokens = cur.Trim().Split();
                        if (!int.TryParse(tokens[sub], out val))
                            val = 0;
                        val += del;
                        if ((val < min) || (val > max))
                            return false;
                        tokens[sub] = val.ToString();
                        cur = string.Join(" ", tokens);
                        return true;
                    default:
                        if (!int.TryParse(cur, out val))
                            val = (min + max) / 2;
                        val += del;
                        if ((val < min) || (val > max))
                            return false;
                        cur = val.ToString();
                        return true;
                }
            return false;
        }

        public void Zero()
        {
            switch (oType)
            {
                case EOptionType.eCheck:
                    bst = "false";
                    break;
                case EOptionType.eString:
                    string[] tokens = cur.Trim().Split();
                    for (int n = 0; n < tokens.Length; n++)
                        tokens[n] = "0";
                    bst = string.Join(" ", tokens);
                    break;
                default:
                    int mid = (min + max) / 2;
                    bst = mid.ToString();
                    break;
            }
        }

        public string GetElements()
        {
            if (oType == EOptionType.eString)
            {
                string result = string.Empty;
                string[] tokens = bst.Trim().Split();
                for (int n = 0; n < tokens.Length; n++)
                    result += $" {n + 1}={tokens[n]}";
                return result.Trim();
            }
            return bst;
        }

    }

    class COptionList : List<COption>
    {
        public int index = 0;
        public int delta = 0;
        public int length = 0;
        public CMix mix = new CMix();
        public string factor = string.Empty;

        public void Init()
        {
            int len = CountFactors();
            mix.SetLen(len);
        }

        public void SaveToIni(CRapIni ini)
        {
            foreach (COption option in this)
                option.SaveToIni(ini);
            ini.Write("mod>index", index);
            ini.Write("mod>delta", delta);
            ini.Write("mod>factor", factor);
            ini.Write("mod>mix", mix);
        }

        public void LoadFromIni(CRapIni ini)
        {
            Clear();
            List<string> ol = ini.ReadKeyList("option");
            foreach (string name in ol)
            {
                COption option = new COption(name);
                option.LoadFromIni(ini);
                if (option.enabled)
                    Add(option);
            }
            index = ini.ReadInt("mod>index");
            delta = ini.ReadInt("mod>delta");
            factor = ini.Read("factor>last");
            mix.SetList(ini.ReadListInt("mod>mix"));
            length = CountFactors();
            if (mix.Count != length * 2)
                mix.SetLen(length);
        }

        public int CountFactors()
        {
            int l = 0;
            foreach (COption option in this)
                    if (option.oType == EOptionType.eString)
                        l += option.cur.Split().Length;
                    else l++;
            return l;
        }

        public COption GetOption(int i)
        {
            if ((i >= 0) && (i < Count))
                return this[i];
            return null;
        }

        public bool GetIndexSub(int i, out int index, out int sub)
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

        public bool Modified()
        {
            return OptionsBst() != OptionsCur();
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
            if (success == 0)
            {
                int value = mix.GetVal(fail);
                delta = 1 << (fail / (length * 2) + value / (length * 2));
                if (value % (length * 2) < length)
                    delta = -delta;
                index = value % length;
            }
            else if (delta < 0)
                delta--;
            else
                delta++;
            GetIndexSub(index, out int idx, out int sub);
            COption opt = this[idx];
            if (opt.Modify(sub, delta))
            {
                factor = $"{opt.name} {opt.cur}";
                Console.WriteLine();
                int result = fail > 0 ? -fail : success;
                if (Modified())
                    Console.WriteLine($">> {bstScore:N2} result {result} delta {delta} {opt.name} {opt.bst} >> {opt.cur}");
                else
                    Console.WriteLine($">> {bstScore:N2}");
                return true;
            }
            return false;
        }

    }

    internal class CMod
    {
        int fail = 0;
        int success = 0;
        int extra = 0;
        public double bstScore = 0;
        public CLast last = new CLast();
        readonly CHisList hl = new CHisList();
        public readonly COptionList optionList = new COptionList();
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
            success = ini.ReadInt("mod>success", success);
            bstScore = ini.ReadDouble("mod>score");
            hl.FromSl(ini.ReadListStr("mod>his", "|"));
            last.FromSl(ini.ReadListStr("mod>last", "|"));
        }

        public void SaveToIni()
        {
            optionList.SaveToIni(ini);
            ini.Write("mod>extra", extra);
            ini.Write("mod>fail", fail);
            ini.Write("mod>success", success);
            ini.Write("mod>score", bstScore);
            ini.Write("mod>his", hl.ToSl(), "|");
            ini.Write("mod>last", last.ToSl(), "|");
            ini.Save();
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

        public bool SetScore()
        {
            double score = Program.accuracy.GetAccuracy();
            bool lastAdd = false;
            if (Program.accuracy.GetProgress() == 100)
                lastAdd = last.AddVal(score);
            if (lastAdd)
                if (!string.IsNullOrEmpty(optionList.factor))
                    log.Add($"{optionList.factor} {score:N2}");
            int oldExtra = extra;
            if (extra > 0)
                extra--;
            if (bstScore < score)
            {
                if ((bstScore > 0) && (extra == 0) && optionList.Modified())
                    success++;
                optionList.CurToBst();
                bstScore = score;
                extra = 0;
                fail = 0;
                hl.RemoveIndex(optionList.index);
                optionList.Init();
                string bst = $"!! {score:N2} {optionList.OptionsBst()}";
                log.Add(bst);
                Console.WriteLine();
                Console.WriteLine(bst);
            }
            else
            {
                success = 0;
                if (hl.Add(score, optionList.index, optionList.delta))
                    if (lastAdd && (oldExtra == 0))
                        extra = 2;
                if (extra > 0)
                {
                    string names = string.Empty;
                    optionList.GetIndexSub(optionList.index, out int idx, out int _);
                    COption opt = optionList.GetOption(idx);
                    if (opt != null)
                        names = $"{opt.name} {optionList.index}";
                    optionList.Modify();
                    int modified = 0;
                    for (int n = 0; n < hl.Count; n++)
                    {
                        CHis h = hl[n];
                        if (h.index != optionList.index)
                        {
                            optionList.GetIndexSub(h.index, out idx, out _);
                            opt = optionList.GetOption(idx);
                            if (opt != null)
                                names += $" {opt.name} {h.index}";
                            optionList.Modify(h.index, h.delta);
                            if (++modified >= extra)
                                break;
                        }
                    }
                    if (extra > modified)
                        extra = modified;
                    Console.WriteLine($"extra {extra} {names}");
                    return modified > 0;
                }
                else
                    fail++;
            }
            SaveToIni();
            return Modify(0);
        }

        public void Reset()
        {
            optionList.Init();
            optionList.BstToCur();
            fail = 0;
            success = 0;
            bstScore = 0;
            hl.Clear();
            SaveToIni();
            Console.WriteLine(optionList.OptionsCur());
        }

        public void Zero()
        {
            foreach (COption opt in optionList)
                opt.Zero();
            Reset();
        }

    }
}
