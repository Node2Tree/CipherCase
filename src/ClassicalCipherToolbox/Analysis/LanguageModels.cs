using System;
using System.Collections.Generic;

namespace ClassicalCipherToolbox.Analysis
{
    internal static class LanguageModels
    {
        private static readonly Dictionary<string, double[]> Frequencies = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "EN", new[] {8.167,1.492,2.782,4.253,12.702,2.228,2.015,6.094,6.966,0.153,0.772,4.025,2.406,6.749,7.507,1.929,0.095,5.987,6.327,9.056,2.758,0.978,2.360,0.150,1.974,0.074} },
            { "FR", new[] {7.636,0.901,3.260,3.669,14.715,1.066,0.866,0.737,7.529,0.613,0.074,5.456,2.968,7.095,5.796,2.521,1.362,6.693,7.948,7.244,6.311,1.838,0.049,0.427,0.128,0.326} },
            { "DE", new[] {6.516,1.886,2.732,5.076,16.396,1.656,3.009,4.577,6.550,0.268,1.417,3.437,2.534,9.776,2.594,0.670,0.018,7.003,7.270,6.154,4.166,0.846,1.921,0.034,0.039,1.134} },
            { "ES", new[] {11.525,2.215,4.019,5.010,12.181,0.692,1.768,0.703,6.247,0.493,0.011,4.967,3.157,6.712,8.683,2.510,0.877,6.871,7.977,4.632,2.927,1.138,0.017,0.215,1.008,0.467} },
            { "IT", new[] {11.745,0.927,4.501,3.736,11.792,1.153,1.644,0.636,10.143,0.011,0.009,6.510,2.512,6.883,9.832,3.056,0.505,6.367,4.981,5.623,3.011,2.097,0.033,0.003,0.020,1.181} },
            { "PT", new[] {14.634,1.043,3.882,4.992,12.570,1.023,1.303,0.781,6.186,0.397,0.015,2.779,4.738,4.446,9.735,2.523,1.204,6.530,6.805,4.336,3.639,1.575,0.037,0.253,0.006,0.470} },
            { "NL", new[] {7.486,1.584,1.242,5.933,18.910,0.805,3.403,2.380,6.499,1.460,2.248,3.568,2.213,10.032,6.063,1.570,0.009,6.411,3.730,6.790,1.990,2.850,1.520,0.036,0.035,1.390} },
            { "SV", new[] {9.383,1.535,1.486,4.702,10.149,2.027,2.862,2.090,5.817,0.614,3.140,5.275,3.471,8.542,4.482,1.839,0.020,8.431,6.590,7.691,1.919,2.415,0.142,0.159,0.708,0.070} },
            { "PL", new[] {8.910,1.470,3.960,3.250,7.660,0.300,1.420,1.080,8.210,2.280,3.510,2.100,2.800,5.520,7.750,3.130,0.140,4.690,4.320,3.980,2.500,0.040,4.650,0.020,3.760,5.640} },
            { "TR", new[] {11.920,2.840,0.960,4.710,8.910,0.460,1.250,1.210,8.600,0.030,4.680,5.920,3.750,7.490,2.480,0.890,0.000,6.720,3.010,3.310,3.240,0.960,0.000,0.000,3.340,1.500} }
        };

        private static readonly Dictionary<string, string> CommonBigrams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "EN", "TH HE IN ER AN RE ON AT EN ND TI ES OR TE OF ED IS IT AL AR ST TO NT NG SE HA AS OU IO LE VE CO ME DE HI RI RO IC NE EA RA CE LI CH LL BE MA SI OM UR" },
            { "FR", "ES DE LE EN RE NT ON ER TE EL AN SE ET LA AI IT ME OU EM IE QU NE CE SA IN UE RA NS DU" },
            { "DE", "ER EN CH DE EI TE IN ND IE GE ST NE BE ES UN RE AN HE AU NG SE IT DI SC" },
            { "ES", "DE EN ES EL LA OS AR UE ER RA ON RE NT AL TE CO CI ST AN OR DO AS SE" },
            { "IT", "DI ER EL LA DE RE ON TO EN NO TA TE TI CO SI LE NE RA AL NT LI" },
            { "PT", "DE ES EN EM DO DA OS AR RA CO NT RE ER TE SE AS OR QU AD" },
            { "NL", "EN ER DE ET EE AN HE IN GE TE ND IJ IE OO ST NE VA" },
            { "SV", "EN ER DE AR ET AN IN TE TT OM RA ST ND NG SO" },
            { "PL", "IE NI ZE CZ PR OW WA NA PO RA ST SZ YC" },
            { "TR", "AR ER LA LE IN EN AN DE DA RI LI IN" }
        };

        private static readonly Dictionary<string, string> CommonTrigrams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "EN", "THE AND ING HER ERE ENT THA NTH WAS ETH FOR DTH HES VER HIS OFT STH OTH RES EVE NOT WIT TIO YOU ION" },
            { "FR", "ENT LES DES QUE EST UNE DAN ION EME AIT OUR ELL RES PAS PAR" },
            { "DE", "DER DIE UND EIN ICH DEN CHE END GEN SCH EIT NDE" },
            { "ES", "QUE DEL LAS LOS EST CON POR UNA ENT ADO ION RES" },
            { "IT", "CHE DEL ELL ENT ION PER NON UNA CON ALL" },
            { "PT", "QUE ENT COM EST PAR ACA NTE DOS UMA" },
            { "NL", "EEN VAN HET DER ING AND VO OOR NIET" },
            { "SV", "OCH DET ATT ING SOM TIL ILL ENA" },
            { "PL", "NIE PRZ CZE EGO OWA ENI JEST" },
            { "TR", "LAR LER BIR INI YOR DAN DEN" }
        };

        private static readonly Dictionary<string, string> CommonFourgrams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "EN", "TION THER THAT OFTH FTHE WITH ATIO MENT IONS THIS HERE OULD IGHT HAVE HICH WHIC CTIO EVER FROM OUGH WERE THEY WILL YOUR THEM THEN INTO" },
            { "FR", "TION MENT QUES ESDE DELA LESD IQUE DANS POUR ELLE AINS ETRE" },
            { "DE", "CHEN NDER DIEE EINE ICHE SCHE UNDE DERD EINDE LICH" },
            { "ES", "DELA QUEE LASE CION ENTE DELO ESTA ADOS PARA CONL" },
            { "IT", "DELL IONE CHEL DELA ENTE DEGL ELLA MENT ALLE NELL" },
            { "PT", "DEQU MENT ACAO PARA COMO ESTA ENTE ADOS QUEA DOSE" },
            { "NL", "EENV VANH HETG DEZE VOOR NIET WORD IJKE NDER DERE" },
            { "SV", "OCHS DETS SOMA INTE NING TILL ENDE ANDR" },
            { "PL", "PRZE NIEJ OWAN DZIE CZAS JEST ENIE KTOR SCIE" },
            { "TR", "LARI LERI INDE BIRL OLAR YORU DANB GIBI ILEB ININ" }
        };

        private static readonly Dictionary<string, Dictionary<int, short>> SparseNgrams = BuildSparseModels();
        private static readonly short[] DenseEnglish = EnglishNgramData.Load();
        private static class FiveGramHolder { internal static readonly EnglishFiveGramModel Model = EnglishNgramData.LoadFiveGrams(); }

        internal static double[] GetFrequencies(string language)
        {
            double[] value;
            return Frequencies.TryGetValue(Normalize(language), out value) ? value : Frequencies["EN"];
        }

        internal static string Normalize(string language)
        {
            string value = (language ?? string.Empty).Trim().ToUpperInvariant();
            return Frequencies.ContainsKey(value) ? value : "EN";
        }

        internal static string[] SupportedLanguages()
        {
            return new[] { "EN", "FR", "DE", "ES", "IT", "PT", "NL", "SV", "PL", "TR" };
        }

        internal static string DetectLanguage(string letters)
        {
            return DetectLanguage(letters, "AUTO");
        }

        internal static string DetectLanguage(string letters, string method)
        {
            string normalized = NormalizeMatchMethod(method, (letters ?? string.Empty).Length); string best = "EN"; double bestScore = double.NegativeInfinity;
            foreach (string language in Frequencies.Keys)
            {
                double score = LanguageMatchScore(letters, language, normalized);
                if (score > bestScore) { bestScore = score; best = language; }
            }
            return best;
        }

        internal static string NormalizeMatchMethod(string method, int length)
        {
            string value = (method ?? string.Empty).Trim().ToUpperInvariant().Replace("-", string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty);
            if (value == "COS" || value == "COSINE" || value == "余弦" || value == "余弦相似度") return "COSINE";
            if (value == "LLR" || value == "LOGLIKELIHOOD" || value == "对数似然" || value == "对数似然比") return "LLR";
            if (value == "CHI" || value == "CHISQUARE" || value == "卡方") return "CHI";
            if (value == "NGRAM" || value == "N-GRAM" || value == "N元" || value == "N元语法") return "NGRAM";
            if (length < 60) return "COSINE"; if (length < 240) return "LLR"; return "NGRAM";
        }

        internal static string MatchMethodLabel(string method, int length)
        {
            string value = NormalizeMatchMethod(method, length); return value == "COSINE" ? "余弦相似度" : value == "LLR" ? "对数似然比" : value == "CHI" ? "卡方" : "N-gram";
        }

        internal static double LanguageMatchScore(string letters, string language, string method)
        {
            int[] counts = new int[26]; int total = 0; foreach (char raw in letters ?? string.Empty) { char value = char.ToUpperInvariant(raw); if (value >= 'A' && value <= 'Z') { counts[value - 'A']++; total++; } } if (total == 0) return double.NegativeInfinity;
            string normalized = NormalizeMatchMethod(method, total); double[] model = GetFrequencies(language);
            if (normalized == "NGRAM") return TextScore(CleanLetters(letters), language) / total;
            if (normalized == "CHI") return -ChiSquare(counts, total, language) / total;
            if (normalized == "COSINE") { double dot = 0, observedNorm = 0, modelNorm = 0; for (int i = 0; i < 26; i++) { double observed = counts[i] / (double)total, expected = model[i] / 100.0; dot += observed * expected; observedNorm += observed * observed; modelNorm += expected * expected; } return dot / Math.Sqrt(Math.Max(1e-15, observedNorm * modelNorm)); }
            double g = 0; for (int i = 0; i < 26; i++) { if (counts[i] == 0) continue; double expected = Math.Max(1e-12, total * model[i] / 100.0); g += 2.0 * counts[i] * Math.Log(counts[i] / expected); } return -g / total;
        }

        internal static string DetectSubstitutionLanguage(int[] counts, int total, string method)
        {
            string[] ranked = RankSubstitutionLanguages(counts, total, method, 1); return ranked.Length == 0 ? "EN" : ranked[0];
        }

        internal static string[] RankSubstitutionLanguages(int[] counts, int total, string method, int limit)
        {
            if (counts == null || total <= 0 || limit <= 0) return new string[0]; int[] observed = (int[])counts.Clone(); Array.Sort(observed); Array.Reverse(observed); string normalized = NormalizeMatchMethod(method, total); if (normalized == "NGRAM") normalized = "LLR"; List<KeyValuePair<string, double>> ranked = new List<KeyValuePair<string, double>>();
            foreach (string language in SupportedLanguages()) ranked.Add(new KeyValuePair<string, double>(language, SubstitutionLanguageScore(observed, total, language, normalized)));
            ranked.Sort(delegate(KeyValuePair<string, double> a, KeyValuePair<string, double> b) { int score = b.Value.CompareTo(a.Value); return score != 0 ? score : string.CompareOrdinal(a.Key, b.Key); }); int count = Math.Min(limit, ranked.Count); string[] result = new string[count]; for (int i = 0; i < count; i++) result[i] = ranked[i].Key; return result;
        }

        private static double SubstitutionLanguageScore(int[] observed, int total, string language, string method) { double[] expected = (double[])GetFrequencies(language).Clone(); Array.Sort(expected); Array.Reverse(expected); if (method == "COSINE") { double dot = 0, a = 0, b = 0; for (int i = 0; i < 26; i++) { double o = observed[i] / (double)total, e = expected[i] / 100.0; dot += o * e; a += o * o; b += e * e; } return dot / Math.Sqrt(Math.Max(1e-15, a * b)); } if (method == "CHI") { double chi = 0; for (int i = 0; i < 26; i++) { double e = Math.Max(.001, total * expected[i] / 100.0), d = observed[i] - e; chi += d * d / e; } return -chi / total; } double g = 0; for (int i = 0; i < 26; i++) { if (observed[i] == 0) continue; double e = Math.Max(1e-12, total * expected[i] / 100.0); g += 2 * observed[i] * Math.Log(observed[i] / e); } return -g / total; }

        private static string CleanLetters(string value) { System.Text.StringBuilder result = new System.Text.StringBuilder(); foreach (char raw in value ?? string.Empty) { char c = char.ToUpperInvariant(raw); if (c >= 'A' && c <= 'Z') result.Append(c); } return result.ToString(); }

        internal static double ChiSquare(int[] counts, int total, string language)
        {
            if (total == 0) return double.MaxValue;
            double[] model = GetFrequencies(language);
            double score = 0;
            for (int i = 0; i < 26; i++)
            {
                double expected = total * model[i] / 100.0;
                double difference = counts[i] - expected;
                score += difference * difference / Math.Max(0.001, expected);
            }
            return score;
        }

        internal static double TextScore(string letters, string language)
        {
            if (string.Equals((language ?? string.Empty).Trim(), "AUTO", StringComparison.OrdinalIgnoreCase)) language = DetectLanguage(letters);
            language = Normalize(language);
            int[] counts = new int[26];
            foreach (char value in letters) if (value >= 'A' && value <= 'Z') counts[value - 'A']++;
            double score = -ChiSquare(counts, letters.Length, language);
            if (language == "EN") return score + DenseEnglishScore(letters);
            Dictionary<int, short> model = SparseNgrams[language];
            for (int length = 2; length <= 4; length++)
            {
                if (letters.Length < length) break;
                int code = 0, factor = 1;
                for (int i = 0; i < length; i++) { code = code * 26 + letters[i] - 'A'; if (i > 0) factor *= 26; }
                short weight; if (model.TryGetValue(GramKey(length, code), out weight)) score += weight;
                for (int i = length; i < letters.Length; i++)
                {
                    code = (code % factor) * 26 + letters[i] - 'A';
                    if (model.TryGetValue(GramKey(length, code), out weight)) score += weight;
                }
            }
            return score;
        }

        internal static double SequenceScore(string letters, string language)
        {
            string clean = CleanLetters(letters); language = Normalize(language); if (clean.Length == 0) return double.NegativeInfinity; int[] counts = new int[26]; foreach (char c in clean) counts[c - 'A']++; return TextScore(clean, language) + ChiSquare(counts, clean.Length, language);
        }

        internal static double SubstitutionScore(string letters, string language)
        {
            string clean = CleanLetters(letters); language = Normalize(language); if (clean.Length == 0) return double.NegativeInfinity;
            if (language != "EN") return SequenceScore(clean, language);
            double score = 0;
            for (int length = 3; length <= 4; length++)
            {
                if (clean.Length < length) break;
                int code = 0, factor = 1; for (int i = 0; i < length; i++) { code = code * 26 + clean[i] - 'A'; if (i > 0) factor *= 26; }
                int offset = length == 3 ? 0 : 17576; double weight = length == 3 ? .22 : 1.0; score += DenseEnglish[offset + code] * weight / 100.0;
                for (int i = length; i < clean.Length; i++) { code = (code % factor) * 26 + clean[i] - 'A'; score += DenseEnglish[offset + code] * weight / 100.0; }
            }
            return score;
        }

        internal static double SpacelessSubstitutionScore(string letters, string language)
        {
            string clean = CleanLetters(letters); language = Normalize(language); if (clean.Length == 0) return double.NegativeInfinity; if (language != "EN" || clean.Length < 5) return SubstitutionScore(clean, language); char[] identity = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(); return SpacelessSubstitutionScore(clean, identity, language);
        }

        internal static double SpacelessSubstitutionScore(string encoded, char[] key, string language)
        {
            language = Normalize(language); if (string.IsNullOrEmpty(encoded)) return double.NegativeInfinity;
            if (language != "EN" || encoded.Length < 5) { System.Text.StringBuilder plain = new System.Text.StringBuilder(encoded.Length); foreach (char c in encoded) plain.Append(key[c - 'A']); return SubstitutionScore(plain.ToString(), language); }
            EnglishFiveGramModel model = FiveGramHolder.Model; int fiveCode = 0, fourCode = 0, threeCode = 0; for (int i = 0; i < 5; i++) fiveCode = fiveCode * 26 + key[encoded[i] - 'A'] - 'A'; for (int i = 1; i < 5; i++) fourCode = fourCode * 26 + key[encoded[i] - 'A'] - 'A'; for (int i = 2; i < 5; i++) threeCode = threeCode * 26 + key[encoded[i] - 'A'] - 'A';
            double score = BackoffFiveGram(fiveCode, fourCode, threeCode, model); const int fiveFactor = 456976, fourFactor = 17576, threeFactor = 676;
            for (int i = 5; i < encoded.Length; i++)
            {
                int letter = key[encoded[i] - 'A'] - 'A'; fiveCode = (fiveCode % fiveFactor) * 26 + letter; fourCode = (fourCode % fourFactor) * 26 + letter; threeCode = (threeCode % threeFactor) * 26 + letter; score += BackoffFiveGram(fiveCode, fourCode, threeCode, model);
            }
            return score;
        }

        private static double BackoffFiveGram(int fiveCode, int fourCode, int threeCode, EnglishFiveGramModel model)
        {
            short value; double four = DenseEnglish[17576 + fourCode] / 100.0, three = DenseEnglish[threeCode] / 100.0;
            if (model.Values.TryGetValue(fiveCode, out value)) return value / 100.0 + four * .10 + three * .025;
            return model.Floor / 100.0 + (four + 8.35) * .55 + (three + 7.62) * .08;
        }

        private static double DenseEnglishScore(string letters)
        {
            double score = letters.Length * .7;
            for (int length = 3; length <= 4; length++)
            {
                if (letters.Length < length) break;
                int code = 0, factor = 1; for (int i = 0; i < length; i++) { code = code * 26 + letters[i] - 'A'; if (i > 0) factor *= 26; }
                int offset = length == 3 ? 0 : 17576, baseline = length == 3 ? 425 : 566; double weight = length == 3 ? .0007 : .002; score += (DenseEnglish[offset + code] + baseline) * weight;
                for (int i = length; i < letters.Length; i++) { code = (code % factor) * 26 + letters[i] - 'A'; score += (DenseEnglish[offset + code] + baseline) * weight; }
            }
            return score;
        }

        private static Dictionary<string, Dictionary<int, short>> BuildSparseModels()
        {
            Dictionary<string, Dictionary<int, short>> result = new Dictionary<string, Dictionary<int, short>>(StringComparer.OrdinalIgnoreCase);
            foreach (string language in Frequencies.Keys)
            {
                Dictionary<int, short> model = new Dictionary<int, short>();
                AddNgrams(model, CommonBigrams[language], 2, 2);
                AddNgrams(model, CommonTrigrams[language], 3, 7);
                AddNgrams(model, CommonFourgrams[language], 4, 18);
                result[language] = model;
            }
            return result;
        }

        private static void AddNgrams(Dictionary<int, short> model, string data, int length, short weight)
        {
            foreach (string gram in data.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (gram.Length != length) continue; int code = 0; bool valid = true;
                foreach (char c in gram) { if (c < 'A' || c > 'Z') { valid = false; break; } code = code * 26 + c - 'A'; }
                if (valid) model[GramKey(length, code)] = weight;
            }
        }

        private static int GramKey(int length, int code) { return length * 1000000 + code; }
    }
}
