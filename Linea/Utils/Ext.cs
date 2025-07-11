using Linea.Args;
using Linea.Args.XML;
using Linea.Command;
using Linea.Commmand.XML;
using MapXML;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Linea.Utils
{
    public static class Ext
    {
        public static int ToValidIndex(int i, int len)
        {
            return ((i % len) + len) % len;
        }

        public static uint NN(int i) => i < 0 ? throw new ArgumentException("Argument must be positive") : (uint)i;
        public static char Last(this StringBuilder sb) => sb[sb.Length - 1];
        public static void ReplaceRun(this StringBuilder sb, int atIndex, char[] run) => ReplaceRun(sb, atIndex, run, 0, run.Length);
        public static void ReplaceRun(this StringBuilder sb, int atIndex, char[] run, int fromIndex, int len)
        {
            for (int i = 0; i < len; i++)
            {
                sb[atIndex + i] = run[fromIndex + i];
            }
        }
        public static void LoadFromXML(this ArgumentDescriptorCollection THIS, Stream xmlSource)
        {
            var opt = XMLDeserializer.OptionsBuilder().AllowImplicitFields(false).IgnoreRootNode(false).Build();
            ArgumentsNode args = new();
            XMLDeserializer des = new(xmlSource, Handler: null, RootNodeOwner: args, opt);
            des.Run();

            args.AddAll(THIS);
        }

        public static void LoadFromXML(this CliCommandHandler THIS,
                                       Stream xmlSource,
                                       Func<string, CommandDelegate> GetDelegates)
        {
            var opt = XMLDeserializer.OptionsBuilder()
                .AllowImplicitFields(false)
                .IgnoreRootNode(false)
                .Build();
            CommandsNode comms = new();
            XMLDeserializer des = new(xmlSource, Handler: null, RootNodeOwner: comms, opt);
            des.Run();
            comms.Create(THIS, GetDelegates);
        }

        public static IEnumerable<string> RowsToFixedLengthStrings(
            this IEnumerable<(string, string, string, string, string)> table2, 
            string separator = "  |  ", 
            int maxColumnLen = int.MaxValue)
        {
            string[][] values = (from row in table2 select row.AllEntries()).ToArray();
            int[] maxLengths = new int[values[0].Length];

            for (int row = 0; row < values.Length; row++)
            {
                string[] dr = values[row];
                for (int col = 0; col < maxLengths.Length; col++)
                {
                    maxLengths[col] = Math.Max(maxLengths[col], dr[col].ToString().Length);
                }
            }

            int totLen = 0;
            for (int i = 0; i < maxLengths.Length; i++)
            {
                totLen += maxLengths[i] = Math.Min(maxColumnLen, maxLengths[i]);
            }
            totLen += separator.Length * (maxLengths.Length - 1);

            List<string> result = new();

            StringBuilder sb = new(totLen);

            for (int row = 0; row < values.Length; row++)
            {
                sb.Clear();
                string[] dr = values[row];

                for (int col = 0; col < maxLengths.Length; col++)
                {
                    if (col > 0) sb.Append(separator);

                    string val = dr[col]?.ToString() ?? "";
                    int diff = maxLengths[col] - val.Length;
                    if (diff < 0) //too long
                        sb.Append(val.Substring(0, maxColumnLen));
                    else if (diff > 0) //too short
                    {
                        sb.Append(val).Append(' ', diff);
                    }
                    else
                    {
                        sb.Append(val);
                    }
                }
                result.Add(sb.ToString());
            }
            return result;
        }

        internal static string? DecryptSecureString(SecureString secureString)
        {
            if (secureString == null) return null;
            IntPtr stringPtr = IntPtr.Zero;
            string? Decrypted = null;
            GCHandle gcHandler = GCHandle.Alloc(Decrypted, GCHandleType.Pinned);

            try
            {
                stringPtr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                Decrypted = Marshal.PtrToStringUni(stringPtr);
                return Decrypted;
            }
            finally
            {
                gcHandler.Free();
                Marshal.ZeroFreeGlobalAllocUnicode(stringPtr);
            }
        }
        internal static SecureString EncryptSecureString(string unsafeString)
        {
            SecureString ss = new();
            foreach (char item in unsafeString)
            {
                ss.AppendChar(item);
            }
            ss.MakeReadOnly();
            return ss;
        }

        internal static SecureString ToSecureString(this string unsafeString) => EncryptSecureString(unsafeString);
        internal static string? ToClearString(this SecureString safeString) => DecryptSecureString(safeString);


        internal static IEnumerable<ResultType> ForEach<EnumerableType, ResultType>(
                    this IEnumerable<EnumerableType> list,
                    Func<EnumerableType, ResultType> function)
        {
            List<ResultType> _result = new();
            list.ForEach(l => _result.Add(function(l)));
            return _result;
        }

        internal static void ForEach<EnumerableType>
            (this IEnumerable<EnumerableType> list, Action<EnumerableType> action)
        {
            foreach (EnumerableType item in list)
            {
                action(item);
            }
        }
        internal static void AddRange<V>(this ISet<V> me, IEnumerable<V> all) => all.ForEach(v => me.Add(v));

        #region Tuples

        internal static V[] AllEntries<V>(this Tuple<V, V, V, V, V, V, V> t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4,t.Item5,t.Item6,t.Item7};
        internal static V[] AllEntries<V>(this Tuple<V, V, V, V, V, V> t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4,t.Item5,t.Item6};
        internal static V[] AllEntries<V>(this Tuple<V, V, V, V, V> t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4,t.Item5};
        internal static V[] AllEntries<V>(this Tuple<V, V, V, V> t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4};
        internal static V[] AllEntries<V>(this Tuple<V, V, V> t) => new V[] {
                                t.Item1,t.Item2,t.Item3};
        internal static V[] AllEntries<V>(Tuple<V, V> t) => new V[] {
                                t.Item1,t.Item2};

        internal static V[] AllEntries<V>(this (V, V, V, V, V, V, V) t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4,t.Item5,t.Item6,t.Item7};
        internal static V[] AllEntries<V>(this (V, V, V, V, V, V) t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4,t.Item5,t.Item6};
        internal static V[] AllEntries<V>(this (V, V, V, V, V) t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4,t.Item5};
        internal static V[] AllEntries<V>(this (V, V, V, V) t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4};
        internal static V[] AllEntries<V>(this (V, V, V) t) => new V[] {
                                t.Item1,t.Item2,t.Item3};
        internal static V[] AllEntries<V>((V, V) t) => new V[] {
                                t.Item1,t.Item2};
        #endregion


    }
}
