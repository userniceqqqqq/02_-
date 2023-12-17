using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    class Helper
    {
        public static Dictionary<string, Dictionary<string, string>> ReadFileForDiscipToParaTypeToGruop(string fileName)
        {
            Dictionary<string, Dictionary<string, string>> dict = new Dictionary<string, Dictionary<string, string>>();

            FileStream fileStream = new FileStream(fileName, FileMode.Open);
            StreamReader streamReader = new StreamReader(fileStream);

            while (!streamReader.EndOfStream)
            {
                string content = streamReader.ReadLine();//逐行读取
                if (content == null || content == "")//排除空白行干扰
                {
                    continue;
                }
                string[] strArray = content.Split(new string[1] { "||" }, StringSplitOptions.None);
                if (!dict.ContainsKey(strArray[0].Trim()))
                {
                    dict.Add(strArray[0].Trim(), new Dictionary<string, string>());
                }
                dict[strArray[0].Trim()].Add(strArray[1].Trim(), strArray[2].Trim());
            }
            fileStream.Close();
            streamReader.Close();

            return dict;
        }


        public static List<BuiltInParameterGroup> ReadFileForEditableBuiltInParaGroup(string fileName)
        {
            List<BuiltInParameterGroup> groups = new List<BuiltInParameterGroup>();

            FileStream fileStream = new FileStream(fileName, FileMode.Open);
            StreamReader streamReader = new StreamReader(fileStream);

            while (!streamReader.EndOfStream)
            {
                string content = streamReader.ReadLine().Trim();//逐行读取
                if (content == null || content == "")//排除空白行干扰
                {
                    continue;
                }

                BuiltInParameterGroup curBuiltInParameterGroup;
                if (Enum.TryParse(content, out curBuiltInParameterGroup))
                {
                    groups.Add(curBuiltInParameterGroup);
                }
            }
            fileStream.Close();
            streamReader.Close();

            return groups;
        }
    }
}
