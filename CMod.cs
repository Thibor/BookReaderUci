using RapIni;
using RapLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

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
            string s = score.ToString(CultureInfo.InvariantCulture);
            return $"{s} {index} {delta}";
        }

        public void FromStr(string s)
        {
            string[] a = s.Split();
            score = Convert.ToDouble(a[0], CultureInfo.InvariantCulture);
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
            return true;
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

    class COption
    {
        public bool enabled = true;
        public string cur = "0";
        public int delta = 64;
        public string bst = "0";
        public string name = string.Empty;
        public EOptionType oType = EOptionType.eSpin;

        public COption(string s)
        {
            name = s;
        }

        public int GetMax(int v)
        {
            if (oType == EOptionType.eCheck)
                return 1;
            return v + delta + Math.Abs(v) / 2;
        }

        public int GetMin(int v)
        {
            if (oType == EOptionType.eCheck)
                return 0;
            return v - delta - Math.Abs(v) / 2;
        }

        public int GetBst()
        {
            int.TryParse(bst, out int val);
            return val;
        }

        public int GetBst(int sub)
        {
            string[] tokens = bst.Trim().Split();
            int.TryParse(tokens[sub], out int val);
            return val;
        }

        string BstToEle()
        {
            string result = string.Empty;
            string[] tokens = bst.Trim().Split();
            for (int i = 0; i < tokens.Length; i++)
                result += $" {i + 1}={tokens[i]}";
            return result.Trim();
        }

        public void SaveToIni(CRapIni ini)
        {
            ini.Write($"option>{name}>enabled", enabled);
            ini.Write($"option>{name}>type", CMod.TypeToStr(oType));
            ini.Write($"option>{name}>delta", delta);
            ini.Write($"option>{name}>cur", cur);
            ini.Write($"option>{name}>bst", bst);
            ini.Write($"option>{name}>ele", BstToEle());
        }

        public void LoadFromIni(CRapIni ini)
        {
            cur = ini.Read($"option>{name}>cur");
            bst = ini.Read($"option>{name}>bst", cur);
            enabled = ini.ReadBool($"option>{name}>enabled", true);
            oType = CMod.StrToType(ini.Read($"option>{name}>type"));
            delta = ini.ReadInt($"option>{name}>delta", delta);
            if (oType == EOptionType.eCheck)
            {
                delta = 1;
            }
        }

        public bool Modify(int sub, int del)
        {
            int vBst, vCur;
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
                        vBst = GetBst(sub);
                        vCur = vBst + del;
                        if ((vCur < GetMin(vBst)) || (vCur > GetMax(vBst)))
                            return false;
                        string[] tokens = bst.Trim().Split();
                        tokens[sub] = vCur.ToString();
                        cur = string.Join(" ", tokens);
                        return true;
                    default:
                        vBst = GetBst();
                        vCur = vBst + del;
                        if ((vCur < GetMin(vBst)) || (vCur > GetMax(vBst)))
                            return false;
                        cur = vCur.ToString();
                        return true;
                }
            return false;
        }

        public bool LoadFromCode(string cod)
        {
            int index = cod.IndexOf('=');
            if (index < 0)
                return false;
            string val = cod.Substring(index + 1).Trim(new[] { ' ', '\t', '\"', ';' });
            string[] tn = cod.Substring(0, index).Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tn.Length != 2)
                return false;
            string type = tn[0];
            name = tn[1];
            bst = val;
            cur = val;
            if (type == "bool")
                oType = EOptionType.eCheck;
            else if (type == "int")
                oType = EOptionType.eSpin;
            else
                oType = EOptionType.eString;
            return true;
        }

        public string SaveToCode()
        {
            string code;
            switch (oType)
            {
                case EOptionType.eCheck:
                    code = $"bool {name} = {bst};";
                    break;
                case EOptionType.eString:
                    code = $"string {name} = \"{bst}\";";
                    break;
                default:
                    code = $"int {name} = {bst};";
                    break;
            }
            return code;
        }

    }

    class COptionList : List<COption>
    {
        public int index = 0;
        public int delta = 0;

        public void SaveToIni(CRapIni ini)
        {
            ini.DeleteKey("option");
            foreach (COption option in this)
                option.SaveToIni(ini);
            ini.Write("mod>index", index);
            ini.Write("mod>delta", delta);
            ini.Save();
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
        }

        public void LoadFromCode()
        {
            Clear();
            string[] lines = File.ReadAllLines("mod.cod");
            foreach (string line in lines)
            {
                COption option = new COption(string.Empty);
                if (option.LoadFromCode(line))
                    Add(option);
            }
            index = 0;
            delta = 0;
        }

        public void SaveToCode()
        {
            List<string> sl = new List<string>();
            foreach (COption option in this)
                sl.Add(option.SaveToCode());
            sl.Sort();
            File.WriteAllLines("mod.cod", sl);
        }

        public int CountEnabled()
        {
            int r = 0;
            foreach (COption option in this)
                if (option.enabled)
                    r++;
            return r;
        }

        public int CountFactors()
        {
            int l = 0;
            foreach (COption option in this)
                if (option.enabled)
                    if (option.oType == EOptionType.eString)
                        l += option.cur.Split().Length;
                    else l++;
            return l;
        }

        public void Enabled(bool v)
        {
            foreach (COption option in this)
                option.enabled = v;
        }

        public COption GetOption(int i)
        {
            if ((i >= 0) && (i < Count))
                return this[i];
            return null;
        }

        public bool GetIndexSubIndex(int index, out int optionIndex, out int optionSubIndex)
        {
            optionIndex = 0;
            optionSubIndex = 0;
            if (Count == 0)
                return false;
            int l = 0;
            index %= CountFactors();
            for (int n = 0; n < Count; n++)
            {
                COption opt = this[n];
                if (opt.oType == EOptionType.eString)
                    l += opt.cur.Split().Length;
                else l++;
                if (l > index)
                {
                    optionIndex = n;
                    optionSubIndex = l - index - 1;
                    break;
                }
            }
            return true;
        }

        public string OptionsCur()
        {
            string mod = string.Empty;
            foreach (COption opt in this)
                if (opt.enabled)
                    mod += $" {opt.name} {opt.cur}";
            return mod;
        }

        public string OptionsBst()
        {
            string mod = string.Empty;
            foreach (COption opt in this)
                if (opt.enabled)
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
            i = i % CountFactors();
            GetIndexSubIndex(i, out int idx, out int sub);
            return this[idx].Modify(sub, d);
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
            for (int n = 0; n < len; n++)
                Add(n);
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


    internal class CMod
    {
        int sign = 0;
        int fail = 0;
        int extra = 0;
        int modIndex = 0;
        public int blunderLimit = 0;
        public double bstScore = 0;
        public double avgProgress = 0;
        public CMix mix = new CMix();
        public CLast last = new CLast();
        readonly CHisList historyList = new CHisList();
        public readonly COptionList optionList = new COptionList();
        public static readonly CRapLog log = new CRapLog("mod.log");
        readonly CRapIni ini = new CRapIni(@"mod.ini");
        static readonly Random rnd = new Random();

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

        public void SaveToIni()
        {
            optionList.SaveToIni(ini);
            ini.Write("mod>extra", extra);
            ini.Write("mod>fail", fail);
            ini.Write("mod>modIndex", modIndex);
            ini.Write("mod>score", bstScore);
            ini.Write("mod>progress>avg", avgProgress);
            ini.Write("mod>his", historyList.ToSl(), "|");
            ini.Write("mod>last", last.ToSl(), "|");
            ini.Write("mod>blunder>limit", blunderLimit);
            ini.Write("mod>sign", sign);
            ini.Write("mod>mix", mix);
            ini.Save();
        }

        void LoadFromIni()
        {
            ini.Load();
            optionList.LoadFromIni(ini);
            extra = ini.ReadInt("mod>extra");
            fail = ini.ReadInt("mod>fail");
            modIndex = ini.ReadInt("mod>modIndex");
            bstScore = ini.ReadDouble("mod>score");
            avgProgress = ini.ReadDouble("mod>progress>avg");
            historyList.FromSl(ini.ReadListStr("mod>his", "|"));
            last.FromSl(ini.ReadListStr("mod>last", "|"));
            blunderLimit = ini.ReadInt("mod>blunder>limit");
            sign = ini.ReadInt("mod>sign");
            mix.SetList(ini.ReadListInt("mod>mix"));
            int length = optionList.CountFactors();
            if (mix.Count != length * 2)
                mix.SetLen(length * 2);

        }

        public void LoadFromCode()
        {
            log.Add("load from code");
            optionList.LoadFromCode();
            Reset();
        }

        public void Enabled(bool v)
        {
            optionList.Enabled(v);
            optionList.SaveToIni(ini);
            ini.Save();
        }

        public void ShowBest()
        {
            foreach (var option in optionList)
                if (option.enabled)
                {
                    Console.WriteLine($"{option.name} {option.bst}");
                }
        }

        public void PrintBlunderLimit()
        {
            int bl = blunderLimit;
            int count = Program.accuracy.Count;
            int b = bl / (count * count);
            bl = bl - b * count * count;
            int m = bl / count;
            int i = bl - m * count;
            Console.WriteLine($"limit blunders {b} mistakes {m} inaccuracies {i}");
        }


        bool Modify(int modTry)
        {
            int countFactors = optionList.CountFactors();
            if (modTry >= countFactors)
                return false;
            int factorIndex = modIndex % countFactors;
            int factorDelta = modIndex / countFactors;
            modIndex++;
            optionList.BstToCur();
            optionList.index = mix.GetVal(factorIndex);
            optionList.GetIndexSubIndex(optionList.index, out int idx, out int sub);
            COption option = optionList[idx];
            optionList.delta = 1 << (factorDelta / 2);
            if ((factorDelta & 1) == sign)
                optionList.delta = -optionList.delta;
            Console.WriteLine();
            if (option.Modify(sub, optionList.delta))
            {
                Console.WriteLine($">> {bstScore:N2} {modIndex * 50 / countFactors}% delta {optionList.delta} {option.name} {option.bst} >> {option.cur}");
                return true;
            }
            return Modify(modTry + 1);
        }

        public bool SetScore()
        {
            double score = Program.accuracy.GetAccuracy();
            bool lastAdd = false;
            if (Program.accuracy.GetProgress() == 100)
                lastAdd = last.AddVal(score);
            if ((blunderLimit == 0) || (bstScore < score) || (blunderLimit > Program.accuracy.BlunderLimit()))
            {
                blunderLimit = Program.accuracy.BlunderLimit();
                PrintBlunderLimit();
            }
            if (bstScore < score)
            {
                bstScore = score;
                modIndex = 0;
                extra = 0;
                fail = 0;
                sign = rnd.Next(2);
                mix.Shuffle();
                optionList.CurToBst();
                optionList.SaveToCode();
                historyList.RemoveIndex(optionList.index);
                SaveToIni();
                log.Add($"!! {score:N2} ({Program.accuracy.blunders} {Program.accuracy.mistakes} {Program.accuracy.inaccuracies} {Program.accuracy.Count})");
                Console.WriteLine();
                Console.WriteLine($"!! {score:N2} {optionList.OptionsBst()}");
            }
            else
            {
                if (historyList.Add(score, optionList.index, optionList.delta))
                    if (lastAdd && (extra == 0))
                        extra = 2;
                if (extra > 0)
                {
                    extra--;
                    string names = string.Empty;
                    optionList.GetIndexSubIndex(optionList.index, out int idx, out int _);
                    COption opt = optionList.GetOption(idx);
                    if (opt != null)
                        names = $"{opt.name} {optionList.index}";
                    optionList.Modify();
                    int modified = 0;
                    for (int n = 0; n < historyList.Count; n++)
                    {
                        CHis h = historyList[n];
                        if (h.index != optionList.index)
                        {
                            optionList.GetIndexSubIndex(h.index, out idx, out _);
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
            }
            return Modify(0);
        }

        public void Reset()
        {
            fail = 0;
            bstScore = 0;
            avgProgress = 0;
            blunderLimit = 0;
            modIndex = 0;
            sign = rnd.Next(2);
            historyList.Clear();
            mix.Shuffle();
            optionList.BstToCur();
            SaveToIni();
            Console.WriteLine(optionList.OptionsCur());
        }

        public double GetAvgProgress()
        {
            double progress = Program.accuracy.GetProgress();
            return avgProgress * 0.9 + progress * 0.1;
        }

        public void SetAvgProgress()
        {
            avgProgress = GetAvgProgress();
        }

    }
}
