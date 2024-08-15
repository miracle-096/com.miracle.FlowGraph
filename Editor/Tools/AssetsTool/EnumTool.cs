using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FlowGraph.Editor
{
    [CreateAssetMenu(menuName = "FlowFrame/EditorTool/EnumTool")]
    public class EnumTool : ScriptableObject
    {
        public MonoScript enumFile;
        [Space]
        [InlineButton(nameof(Inject))]
        public string enumInject;
        [InlineButton(nameof(Remove))]
        [ValueDropdown(nameof(GetAllTypeFromThisEnum))]
        public string enumRemove;

        public void Inject()
        {
            if(enumFile != null && !string.IsNullOrEmpty(enumInject))
            {
                var list = GetAllTypeFromThisEnum();
                foreach (var item in list)
                {
                    if (item.Equals(enumInject))
                    {
                        Debug.Log("��ö���Ѵ���");
                        return;
                    }
                }

                AddEnumValue(enumInject, AssetDatabase.GetAssetPath(enumFile));
                AssetDatabase.Refresh();
                // CompilationPipeline.RequestScriptCompilation();
                OnValidate();
            }
        }

        //�����ʹ��ö���Ƴ����ܡ���Unity Inspector����ö��ʱʹ�õ���������ű��
        //�Ƴ��м��ö������ʱ���ᵼ�º�����ö��˳����ǰ�ƶ�һλ����ǰ��������������

        /// <summary>
        /// ö���Ƴ�
        /// </summary>
        public void Remove()
        {
            if (enumFile != null && !string.IsNullOrEmpty(enumRemove))
            {
                RemoveEnumValue(enumRemove, AssetDatabase.GetAssetPath(enumFile));
                AssetDatabase.Refresh();
                //CompilationPipeline.RequestScriptCompilation();
                OnValidate();
            }
            
        }

        public List<string> GetAllTypeFromThisEnum()
        {
            if (enumFile == null || enumFile == null)
                return null;

            //��ö�ٽű��и����namespace��ʹ��getclass�᷵�ؿ����ñ���...
            //�鵽������unity monoscript��һ��bug����2022.2.x�汾���޸��ˡ������ȡ�б���ʹ���ı����������

            //if (enumFile.GetClass().IsEnum)
            //{
            //    return Enum.GetNames(enumFile.GetClass())?.ToList();
            //}

            string[] lines = enumFile.text.Split("\n");

            List<string> ret = new();
            bool isEnum = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Contains($"enum"))
                {
                    isEnum = true;
                }
                if (line.Contains(",") && isEnum)
                {
                    ret.Add(lines[i].Trim().Replace(",", ""));
                }
            }
            return ret;
        }

        
        [TextArea(20, 30), ReadOnly]
        [Title("Code Preview"), PropertyOrder(100)]
        public string code;

        protected virtual void OnValidate()
        {
            code = enumFile == null ? "ȱ��ģ��ű�" : enumFile.text;
        }

        public static void AddEnumValue(string newEnumValue, string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);

            bool inEnum = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];//.Trim();

                if (inEnum && line.Contains("}"))
                {
                    lines[i] = lines[i-1].Substring(0, lines[i-1].Length - lines[i - 1].TrimStart().Length) + $"{newEnumValue},\r\n{line}";
                    inEnum = false;
                    break;
                }

                if (line.Contains($"enum"))
                {
                    inEnum = true;
                }
                lines[i] = lines[i].Replace("\r", "").Replace("\n", "\r\n");
            }

            File.WriteAllLines(filePath, lines);
        }
        public static void RemoveEnumValue(string enumValueToRemove, string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.Contains(enumValueToRemove + ","))
                {
                    lines[i] = "";
                    Debug.Log("���Ƴ���Ӧö��");
                }
                lines[i] = lines[i].Replace("\r", "").Replace("\n", "\r\n");
            }
            var list = lines.ToList();
            //�����Ŀ�����Ƴ�����
            list.RemoveAll(x => x.Trim() == "\r\n" || string.IsNullOrWhiteSpace(x.Trim()));

            File.WriteAllLines(filePath, list);
        }
    }
}