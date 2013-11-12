using System;
using System.IO;
using System.Xml.Serialization;

namespace RtvSlo.Core.HelperExtensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Checks if object is null and throw exception
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        public static void NullCheck(this object obj, string name="")
        {
            if (obj == null)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentNullException();
                }
                else
                {
                    throw new ArgumentNullException(name);
                }
            }
        }

        /// <summary>
        /// Serialize serializable object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toSerialize"></param>
        /// <returns></returns>
        public static string SerializeObject<T>(this T toSerialize)
        {
            if (toSerialize == null)
            {
                return "[null]";
            }

            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());
                StringWriter textWriter = new StringWriter();

                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
            catch (Exception ex)
            {
                return "[serialize exception]";
            }
        }

        /// <summary>
        /// Safe trim string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string SafeTrim(this object str)
        {
            string result = str as string;
            if (string.IsNullOrEmpty(result))
            {
                return string.Empty;
            }
            else
            {
                return result.Trim();
            }
        }

        public static string ToSafeString(this object obj)
        {
            if (obj == null)
            {
                return null;
            }
            else
            {
                return obj.ToString();
            }
        }
    }
}
