using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Godot;

namespace Moinros.CSharp.Util
{

    /// <summary>
    /// 通用函数库
    /// </summary>
    public static partial class UniversalUtil
    {

        /// <summary>
        /// 数字正则表达式
        /// </summary>
        public const string RegexNumber = @"^-?[0-9]+(\.[0-9]+)?$";

        /// <summary>
        /// 数字正则表达式
        /// </summary>
        public const string RegexDrop = @"^-?[0-9]+\.$";

        /// <summary>
        /// 判断字符串是否是数字
        /// </summary>
        public static bool NumberStringCheck(string text)
        {
            return MyRegexNumber().IsMatch(text);
        }
        /// <summary>
        ///	字符串转成数字
        /// </summary>
        /// <returns>转换后的数字</returns>
        public static int StringToInt(string str)
        {
            if (str != null && str != "")
            {
                string text = MyRegexDrop().Replace(str, "");
                int value = text == null ? 0 : int.Parse(text);
                return value;
            }
            return 0;
        }
        /// <summary>
        /// 合并数组
        /// </summary>
        public static T[] MergeArray<T>(T[][] arr)
        {
            List<T> list = [];
            for (int i = 0; i < arr.Length; i++)
            {
                list.AddRange(arr[i]);
            }
            return [.. list];
        }

        /// <summary>
        /// 获得当前系统时间
        /// </summary>
        public static long GetSystemTime()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        readonly static JsonSerializerOptions Options = new() { WriteIndented = true };
        /// <summary>
        /// 把对象转为json格式，并写入指定路径
        /// </summary>
        /// <param name="path"></param>
        /// <param name="obj"></param>
        public static void CreateJsonFile(string path, object obj)
        {
            byte[] jsonData = JsonSerializer.SerializeToUtf8Bytes(obj, Options);
            File.WriteAllBytes(path, jsonData);
        }

        /// <summary>
        /// 读取json文件，并转为指定类型的对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        public static T ReadJsonFile<T>(string path)
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json, Options);
            }
            else
            {
                GD.PrintErr($"File not found: {path}");
            }
            return default;
        }

        [GeneratedRegex(RegexDrop)]
        private static partial Regex MyRegexDrop();
        [GeneratedRegex(RegexNumber)]
        private static partial Regex MyRegexNumber();
    }
}