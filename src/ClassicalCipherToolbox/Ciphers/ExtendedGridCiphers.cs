using System;
using System.Collections.Generic;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class FourSquareCipher
    {
        private const string Normal = "ABCDEFGHIKLMNOPQRSTUVWXYZ";
        internal static string Transform(string input, string key1, string key2, bool decrypt)
        {
            string a = CipherUtilities.KeyedAlphabet(key1, false), b = CipherUtilities.KeyedAlphabet(key2, false), text = Normalize(input);
            if (text.Length % 2 != 0) { if (decrypt) throw new CipherException("密文长度须为偶数"); text += "X"; }
            StringBuilder result = new StringBuilder(text.Length);
            for (int i=0;i<text.Length;i+=2)
            {
                if(!decrypt){int p=Normal.IndexOf(text[i]),q=Normal.IndexOf(text[i+1]);result.Append(a[(p/5)*5+q%5]).Append(b[(q/5)*5+p%5]);}
                else{int p=a.IndexOf(text[i]),q=b.IndexOf(text[i+1]);result.Append(Normal[(p/5)*5+q%5]).Append(Normal[(q/5)*5+p%5]);}
            }
            return result.ToString();
        }
        internal static string Normalize(string input){StringBuilder r=new StringBuilder();foreach(char raw in input??string.Empty){char c=char.ToUpperInvariant(raw);if(c>='A'&&c<='Z')r.Append(c=='J'?'I':c);}return r.ToString();}
    }

    internal static class TwoSquareCipher
    {
        internal static string Transform(string input,string key1,string key2,bool decrypt)
        {
            string left=CipherUtilities.KeyedAlphabet(key1,false),right=CipherUtilities.KeyedAlphabet(key2,false),text=FourSquareCipher.Normalize(input);
            if(text.Length%2!=0){if(decrypt)throw new CipherException("密文长度须为偶数");text+="X";}StringBuilder result=new StringBuilder(text.Length);
            for(int i=0;i<text.Length;i+=2){int p=left.IndexOf(text[i]),q=right.IndexOf(text[i+1]);int pr=p/5,pc=p%5,qr=q/5,qc=q%5;if(pr==qr)result.Append(text[i]).Append(text[i+1]);else result.Append(left[qr*5+pc]).Append(right[pr*5+qc]);}return result.ToString();
        }
    }

    internal static class NihilistCipher
    {
        internal static string Encrypt(string input,string squareKey,string cipherKey)
        {
            string square=CipherUtilities.KeyedAlphabet(squareKey,false),key=FourSquareCipher.Normalize(cipherKey);if(key.Length==0)throw new CipherException("请输入加法密钥");StringBuilder r=new StringBuilder();int p=0;
            foreach(char c in FourSquareCipher.Normalize(input)){int a=square.IndexOf(c),b=square.IndexOf(key[p++%key.Length]);if(r.Length>0)r.Append(' ');r.Append((a/5+1)*10+a%5+1+(b/5+1)*10+b%5+1);}return r.ToString();
        }
        internal static string Decrypt(string input,string squareKey,string cipherKey)
        {
            string square=CipherUtilities.KeyedAlphabet(squareKey,false),key=FourSquareCipher.Normalize(cipherKey);if(key.Length==0)throw new CipherException("请输入加法密钥");string[] parts=(input??string.Empty).Split(new[]{' ',',',';','\r','\n','\t'},StringSplitOptions.RemoveEmptyEntries);StringBuilder r=new StringBuilder();
            for(int i=0;i<parts.Length;i++){int value;if(!int.TryParse(parts[i],out value))throw new CipherException("密文须为数字序列");int k=square.IndexOf(key[i%key.Length]);int coord=value-((k/5+1)*10+k%5+1);int row=coord/10-1,col=coord%10-1;if(row<0||row>4||col<0||col>4)throw new CipherException("数字坐标无效");r.Append(square[row*5+col]);}return r.ToString();
        }
    }

    internal static class BazeriesCipher
    {
        private const string Normal="ABCDEFGHIKLMNOPQRSTUVWXYZ";
        internal static string Transform(string input,string numberText,string key,bool decrypt)
        {
            int number;if(!int.TryParse(numberText,out number)||number<1)throw new CipherException("分组数字须为正整数");string square=CipherUtilities.KeyedAlphabet(key,false),text=FourSquareCipher.Normalize(input);StringBuilder substituted=new StringBuilder();
            foreach(char c in text){int index=(decrypt?square:Normal).IndexOf(c);substituted.Append((decrypt?Normal:square)[index]);}
            string digits=number.ToString();StringBuilder result=new StringBuilder();int pos=0,di=0;while(pos<substituted.Length){int size=digits[di++%digits.Length]-'0';if(size==0)size=10;size=Math.Min(size,substituted.Length-pos);for(int i=size-1;i>=0;i--)result.Append(substituted[pos+i]);pos+=size;}return result.ToString();
        }
    }

    internal static class FractionatedMorseCipher
    {
        private const string Alphabet="ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly string[] Morse={".-","-...","-.-.","-..",".","..-.","--.","....","..",".---","-.-",".-..","--","-.","---",".--.","--.-",".-.","...","-","..-","...-",".--","-..-","-.--","--.."};
        internal static string Encrypt(string input,string key)
        {
            string alphabet=CipherUtilities.KeyedAlphabet(key,true),letters=CipherUtilities.Letters(input);StringBuilder stream=new StringBuilder();for(int i=0;i<letters.Length;i++){stream.Append(Morse[letters[i]-'A']).Append('x');}while(stream.Length%3!=0)stream.Append('x');StringBuilder result=new StringBuilder();for(int i=0;i<stream.Length;i+=3){int index=TrigramIndex(stream.ToString().Substring(i,3));if(index<26)result.Append(alphabet[index]);}return result.ToString();
        }
        internal static string Decrypt(string input,string key)
        {
            string alphabet=CipherUtilities.KeyedAlphabet(key,true);StringBuilder stream=new StringBuilder();foreach(char c in CipherUtilities.Letters(input)){int index=alphabet.IndexOf(c);if(index>=0)stream.Append(IndexTrigram(index));}string[] parts=stream.ToString().TrimEnd('x').Split(new[]{'x'},StringSplitOptions.RemoveEmptyEntries);StringBuilder result=new StringBuilder();foreach(string part in parts){int index=Array.IndexOf(Morse,part);result.Append(index>=0?Alphabet[index]:'?');}return result.ToString();
        }
        private static int TrigramIndex(string value){int n=0;foreach(char c in value)n=n*3+(c=='.'?0:c=='-'?1:2);return n;}
        private static string IndexTrigram(int value){char[] r=new char[3];for(int i=2;i>=0;i--){int d=value%3;r[i]=d==0?'.':d==1?'-':'x';value/=3;}return new string(r);}
    }

    internal static class HomophonicCipher
    {
        internal static string Encrypt(string input,string key)
        {
            int[] order=CodeOrder(key);StringBuilder result=new StringBuilder();int[] used=new int[26];foreach(char raw in input??string.Empty){char c=char.ToUpperInvariant(raw);if(c<'A'||c>'Z'){result.Append(raw);continue;}int letter=c-'A',slot=used[letter]++%3,code=order[letter*3+slot];if(result.Length>0&&char.IsDigit(result[result.Length-1]))result.Append(' ');result.Append(code.ToString("00"));}return result.ToString();
        }
        internal static string Decrypt(string input,string key)
        {
            int[] order=CodeOrder(key);Dictionary<int,char> map=new Dictionary<int,char>();for(int l=0;l<26;l++)for(int s=0;s<3;s++)map[order[l*3+s]]=(char)('A'+l);string[] parts=(input??string.Empty).Split(new[]{' ',',',';','\r','\n','\t'},StringSplitOptions.RemoveEmptyEntries);StringBuilder result=new StringBuilder();foreach(string part in parts){int code;if(int.TryParse(part,out code)&&map.ContainsKey(code))result.Append(map[code]);else result.Append(part);}return result.ToString();
        }
        private static int[] CodeOrder(string key){List<int> values=new List<int>();for(int i=0;i<100;i++)values.Add(i);int seed=17;foreach(char c in key??string.Empty)seed=seed*31+c;Random random=new Random(seed);for(int i=values.Count-1;i>0;i--){int j=random.Next(i+1),v=values[i];values[i]=values[j];values[j]=v;}return values.ToArray();}
    }
}
