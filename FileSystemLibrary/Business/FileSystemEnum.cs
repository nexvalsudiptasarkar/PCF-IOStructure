using System;
using System.ComponentModel;
using System.Reflection;

namespace FileSystemLib
{
    internal static class EnumHelper
    {
        public static string GetDescription(this Enum en)
        {
            FieldInfo fi = en.GetType().GetField(en.ToString());
            DescriptionAttribute[] attributes =
                  (DescriptionAttribute[])fi.GetCustomAttributes(
                  typeof(DescriptionAttribute), false);
            return (attributes.Length > 0) ? attributes[0].Description : null;
        }

        internal static T ConvertToEnum<T>(this string EnumCode)
        {
            T enRet = default(T);
            try
            {
                int ECode = 0;
                if (int.TryParse(EnumCode, out ECode))
                    enRet = ECode.ConvertToEnum<T>();
            }
            catch (Exception) { }
            return enRet;
        }

        private static T ConvertToEnum<T>(this int EnumCode)
        {
            T enRet = default(T);
            try
            {
                Type type = typeof(T);
                if (Enum.IsDefined(type, EnumCode))
                {
                    object objEnum = Enum.Parse(typeof(T), EnumCode.ToString());
                    if (objEnum != null) enRet = (T)objEnum;
                }
            }
            catch (Exception) { }
            return enRet;
        }
    }

    public enum Slash
    {
        [Description("/")]
        FrontSlash = 1,
        [Description("\\")]
        BackSlash = 2
    }

    public enum FileSystemType
    {
        [Description("NetworkVault")]
        NetworkVault = 0,
        [Description("HadoopRest")]
        HadoopRest = 1,
        [Description("AWS_S3")]
        AWS_S3 = 2,
        [Description("NoImplementation")]
        NoImplementation = 3,
        [Description("WebDav")]
        WebDav = 4,
        [Description("Etc")]
        Etc = 5
    }
}
