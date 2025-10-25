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
        public int factorIndex;
        public int factorDelta;

        public string ToStr()
        {
            string s = score.ToString(CultureInfo.InvariantCulture);
            return $"{s} {factorIndex} {factorDelta}";
        }

        public void FromStr(string s)
        {
            string[] a = s.Split();
            score = Convert.ToDouble(a[0], CultureInfo.InvariantCulture);
            factorIndex = Convert.ToInt32(a[1]);
            factorDelta = Convert.ToInt32(a[2]);
        }

    }

    class CHisList : List<CHis>
    {

        public bool AddHis(CHis his)
        {
            if (his.factorDelta == 0)
                return false;
            for (int n = Count - 1; n >= 0; n--)
            {
                CHis h = this[n];
                if (h.factorIndex == his.factorIndex)
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
                factorIndex = index,
                factorDelta = delta
            };
            return AddHis(h);
        }

        public bool RemoveIndex(int i)
        {
            for (int n = Count - 1; n >= 0; n--)
                if (this[n].factorIndex == i)
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

        public void SaveToIni(CRapIni ini)
        {
            ini.Write($"option>{name}>enabled", enabled);
            ini.Write($"option>{name}>type", CMod.TypeToStr(oType));
            ini.Write($"option>{name}>delta", delta);
            ini.Write($"option>{name}>cur", cur);
            ini.Write($"option>{name}>bst", bst);
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
        public int factorIndex = 0;
        public int factorDelta = 0;

        public void SaveToIni(CRapIni ini)
        {
            ini.DeleteKey("option");
            foreach (COption option in this)
                option.SaveToIni(ini);
            ini.Write("mod>factor>index", factorIndex);
            ini.Write("mod>factor>delta", factorDelta);
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
            factorIndex = ini.ReadInt("mod>factor>index");
            factorDelta = ini.ReadInt("mod>factor>delta");
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
            factorIndex = 0;
            factorDelta = 0;
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

        public COption GetOption(int optIndex)
        {
            if ((optIndex >= 0) && (optIndex < Count))
                return this[optIndex];
            return null;
        }

        public COption GetOption()
        {
            return FactorToOption(factorIndex);
        }

        public COption FactorToOption(int fi)
        {
            GetOptIndexOptDelta(fi, out int optIndex, out _);
            return GetOption(optIndex);
        }

        public bool GetOptIndexOptDelta(int indexTotal, out int optionIndex, out int optionDelta)
        {
            optionIndex = 0;
            optionDelta = 0;
            if (Count == 0)
                return false;
            int l = 0;
            indexTotal %= CountFactors();
            for (int n = 0; n < Count; n++)
            {
                COption opt = this[n];
                if (opt.oType == EOptionType.eString)
                    l += opt.cur.Split().Length;
                else l++;
                if (l > indexTotal)
                {
                    optionIndex = n;
                    optionDelta = l - indexTotal - 1;
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

        public bool Modify()
        {
            BstToCur();
            return Modify(factorIndex, factorDelta);
        }

        public bool Modified()
        {
            return OptionsBst() != OptionsCur();
        }

        public bool Modify(int factorIndex, int factorDelta)
        {
            factorIndex = factorIndex % CountFactors();
            GetOptIndexOptDelta(factorIndex, out int idx, out int sub);
            return this[idx].Modify(sub, factorDelta);
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
        int fail = 0;
        int extra = 0;
        int modIndex = 0;
        int margin = 0;
        public int limitB = 0;
        public int limitM = 0;
        public int limitI = 0;
        public int progressCount = 0;
        int bestB = 0;
        int bestM = 0;
        int bestI = 0;
        public double bstScore = 0;
        public double progressTotal = 0;
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
            ini.Write("mod>margin", margin);
            ini.Write("mod>bestB", bestB);
            ini.Write("mod>bestM", bestM);
            ini.Write("mod>bestI", bestI);
            ini.Write("mod>progressCount", progressCount);
            ini.Write("mod>score", bstScore);
            ini.Write("mod>progressTotal", progressTotal);
            ini.Write("mod>his", historyList.ToSl(), "|");
            ini.Write("mod>last", last.ToSl(), "|");
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
            margin = ini.ReadInt("mod>margin");
            bestB = ini.ReadInt("mod>bestB");
            bestM = ini.ReadInt("mod>bestM");
            bestI = ini.ReadInt("mod>bestI");
            progressCount = ini.ReadInt("mod>progressCount");
            bstScore = ini.ReadDouble("mod>score");
            progressTotal = ini.ReadDouble("mod>progressTotal");
            historyList.FromSl(ini.ReadListStr("mod>his", "|"));
            last.FromSl(ini.ReadListStr("mod>last", "|"));
            mix.SetList(ini.ReadListInt("mod>mix"));
            int length = optionList.CountFactors();
            if (mix.Count != length * 2)
                mix.SetLen(length * 2);
            UpdateLimit();

        }

        void UpdateLimit()
        {
            limitB = bestB + margin;
            limitM = bestM + margin * 2;
            limitI = bestI + margin * 4;
        }

        public bool ActionLimit()
        {
            int marginB = bestB - Program.accuracy.blunders;
            if (limitB > 0)
                if (Program.accuracy.blunders > limitB)
                    return true;
            if (limitM > 0)
                if (Program.accuracy.mistakes > limitM + marginB * 2)
                    return true;
            if (limitI > 0)
                if (Program.accuracy.inaccuracies > limitI + marginB * 4)
                    return true;
            return false;
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

        public void PrintLimit()
        {
            Console.WriteLine($"limit blunders {limitB} mistakes {limitM} inaccuracies {limitI}");
        }


        bool Modify(int modTry)
        {
            int countFactors = optionList.CountFactors();
            if (++modTry > countFactors)
                return false;
            if (mix.Count == 0)
                return false;
            int factorIndex = modIndex % mix.Count;
            int factorDelta = modIndex / mix.Count;
            int mixIndex = mix.GetVal(factorIndex);
            modIndex++;
            optionList.BstToCur();
            optionList.factorIndex = mixIndex % countFactors;
            optionList.factorDelta = 1 << factorDelta;
            if (mixIndex >= countFactors)
                optionList.factorDelta = -optionList.factorDelta;
            if (optionList.Modify())
            {
                COption option = optionList.GetOption();
                if (option == null)
                    return false;
                Console.WriteLine();
                Console.WriteLine($">> {bstScore:N2} {modIndex * 50 / countFactors}% fIndex: {optionList.factorIndex} fDelta: {optionList.factorDelta} {option.name} {option.bst} >> {option.cur}");
                return true;
            }
            return Modify(modTry);
        }

        void Clear()
        {
            modIndex = 0;
            extra = 0;
            fail = 0;
            mix.Shuffle();
            SaveToIni();
        }

        public bool SetScore()
        {
            double score = Program.accuracy.GetAccuracy();
            bool lastAdd = (extra == 0) && (Program.accuracy.GetProgress() == 100);
            if (extra > 0)
                extra--;
            if (lastAdd)
                last.AddVal(score);
            if (bstScore < score)
            {
                bstScore = score;
                bestB = Program.accuracy.blunders;
                bestM = Program.accuracy.mistakes;
                bestI = Program.accuracy.inaccuracies;
                UpdateLimit();
                PrintLimit();
                optionList.CurToBst();
                optionList.SaveToCode();
                historyList.RemoveIndex(optionList.factorIndex);
                Clear();
                log.Add($"!! {score:N2} ({Program.accuracy.blunders} {Program.accuracy.mistakes} {Program.accuracy.inaccuracies} {Program.accuracy.Count})");
                Console.WriteLine();
                Console.WriteLine($"!! {score:N2} {optionList.OptionsBst()}");
            }
            else
            {
                if (lastAdd && historyList.Add(score, optionList.factorIndex, optionList.factorDelta))
                    extra = 2;
                if (extra > 0)
                {
                    string names = string.Empty;
                    optionList.GetOptIndexOptDelta(optionList.factorIndex, out int oIndex, out int _);
                    COption opt = optionList.GetOption(oIndex);
                    if (opt != null)
                        names = $"{opt.name} {optionList.factorIndex}";
                    optionList.Modify();
                    int modified = 0;
                    for (int n = 0; n < historyList.Count; n++)
                    {
                        CHis h = historyList[n];
                        if (h.factorIndex != optionList.factorIndex)
                        {
                            optionList.GetOptIndexOptDelta(h.factorIndex, out oIndex, out _);
                            opt = optionList.GetOption(oIndex);
                            if (opt != null)
                                names += $" {opt.name} {h.factorIndex}";
                            optionList.Modify(h.factorIndex, h.factorDelta);
                            if (++modified >= extra)
                                break;
                        }
                    }
                    if (extra > modified)
                        extra = modified;
                    if (extra > 0)
                    {
                        Console.WriteLine($"extra {extra} {names}");
                        return true;
                    }
                }
            }
            return Modify(0);
        }

        public void Reset()
        {
            bstScore = 0;
            bestB = 0;
            bestM = 0;
            bestI = 0;
            limitB = 0;
            limitM = 0;
            limitI = 0;
            progressTotal = 0;
            progressCount = 0;
            last.Clear();
            historyList.Clear();
            optionList.BstToCur();
            Clear();
            Console.WriteLine(optionList.OptionsCur());
        }

        public double GetProgress()
        {
            double progress = Program.accuracy.GetProgress();
            return (progressTotal + progress) / (progressCount + 1);
        }

        public void SetProgress()
        {
            progressTotal += Program.accuracy.GetProgress();
            progressCount++;
        }

    }
}
