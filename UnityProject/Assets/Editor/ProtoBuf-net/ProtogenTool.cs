namespace TEngine.Editor
{
#if UNITY_EDITOR
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using Debug = UnityEngine.Debug;

    public static class EditorProtogenTool
    {
        [MenuItem("Tools/生成所有proto文件的CSharp代码")]
        public static void GenerateProtos()
        {
            // 获取绝对路径确保路径一致性
            string protoFolder = Path.GetFullPath("../protos");
            string protogenTool = Path.GetFullPath("../Tools/protogen/protogen.exe");
            string protoScriptFolder = Path.GetFullPath("./Assets/GameScripts/HotFix/GameProto/Proto");

            // 清空代码文件夹下所有cs文件
            if (Directory.Exists(protoScriptFolder))
            {
                Directory.Delete(protoScriptFolder, true);
                Directory.CreateDirectory(protoScriptFolder); // 立即重建目录
            }
            else
            {
                Directory.CreateDirectory(protoScriptFolder);
            }

            // 获取所有proto文件（包括子目录）
            if (!Directory.Exists(protoFolder))
            {
                Debug.Log($"Proto目录不存在: {protoFolder}");
                return;
            }

            var protoFiles = Directory.GetFiles(protoFolder, "*.proto", SearchOption.AllDirectories);
            if (protoFiles.Length == 0)
            {
                Debug.LogWarning("未找到任何proto文件");
                return;
            }

            // 构建protogen命令
            StringBuilder cmdArgs = new StringBuilder();
            cmdArgs.Append(" +names=original");
            cmdArgs.Append(" --proto_path=\"");
            cmdArgs.Append(protoFolder);
            cmdArgs.Append("\"");
            cmdArgs.Append(" --csharp_out=\"");
            cmdArgs.Append(protoScriptFolder);
            cmdArgs.Append("\"");

            // 添加所有proto文件（使用相对路径）
            foreach (string file in protoFiles)
            {
                // 获取相对于proto根目录的相对路径
                string relativePath = Path.GetRelativePath(protoFolder, file)
                    .Replace(Path.DirectorySeparatorChar, '/'); // 统一使用正斜杠

                cmdArgs.Append(" \"");
                cmdArgs.Append(relativePath);
                cmdArgs.Append("\"");
            }

            Debug.Log("执行命令:" + protogenTool + cmdArgs.ToString());

            // 配置进程启动信息
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = protogenTool,
                Arguments = cmdArgs.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // 执行命令
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                // 输出日志
                string output = process.StandardOutput.ReadToEnd();
                string errors = process.StandardError.ReadToEnd();

                if (!string.IsNullOrEmpty(output))
                    Debug.Log("Protogen输出: " + output);

                if (process.ExitCode != 0)
                {
                    Debug.LogError($"Protogen失败! 退出代码: {process.ExitCode}");
                    if (!string.IsNullOrEmpty(errors))
                        Debug.LogError("错误信息: " + errors);
                }
                else
                {
                    Debug.Log($"成功生成 {protoFiles.Length} 个proto文件");
                }
            }

            // 刷新Unity工程
            AssetDatabase.Refresh();
        }
    }
#endif
}