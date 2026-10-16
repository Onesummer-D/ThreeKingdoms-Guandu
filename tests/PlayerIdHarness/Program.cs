using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public static class PlayerPrefs
    {
        private static Dictionary<string,string> values=new Dictionary<string,string>();
        public static string GetString(string key,string fallback="") =>
            values.TryGetValue(key,out var val)?val:fallback;
        public static void SetString(string key,string value)=>values[key]=value;
        public static void Save(){}
    }
}
internal static class Program
{
    static int passed;
    static void Check(bool ok,string title) {
        if(!ok) throw new Exception("FAIL "+title);
        passed++; Console.WriteLine("PASS "+title);
    }
    static void Main()
    {
        Check(!PlayerIdentityState.HasId, "First launch requires an ID");
        Check(PlayerIdentityState.MinLength == 3 && PlayerIdentityState.MaxLength == 18,"Explicit local ID length range");
        Check(!PlayerIdentityState.TrySave("ab",out _),"Too short ID rejected");
        Check(!PlayerIdentityState.TrySave("abcdefghijklmnopqrs",out _),"Overlong ID rejected");
        Check(!PlayerIdentityState.TrySave("含汉字",out _),"Only simple poster-safe ASCII IDs allowed");
        Check(!PlayerIdentityState.TrySave("test<script>",out _),"Markup is not allowed in a poster ID");
        Check(!PlayerIdentityState.TrySave("john doe",out _),"Spaces are not accepted");
        Check(PlayerIdentityState.TrySave(" Guandu_07 ",out _),"Valid ASCII custom ID accepted with trim");
        Check(PlayerIdentityState.CurrentId=="Guandu_07","ID restored from PlayerPrefs");
        Check(PlayerIdentityState.HasId,"Returning player skips first registration");
        Check(PlayerIdentityState.TrySave("Another-12",out _),"User can change local ID from main menu");
        Check(PlayerIdentityState.CurrentId=="Another-12","Updated ID used for poster signature");
        Check(!PlayerIdentityState.TrySave("!",out _) &&
              PlayerIdentityState.CurrentId=="Another-12","Invalid changes never overwrite existing ID");
        Console.WriteLine("player-id-tests: "+passed+"/"+passed);
    }
}
