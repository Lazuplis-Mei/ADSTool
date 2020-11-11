using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Internal;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ADSTool
{

    /// <summary>
    /// 反射工具类
    /// </summary>
    public static class ReflectExtension
    {
        /// <summary>
        /// 调用静态方法
        /// </summary>
        /// <param name="m"></param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static object StaticInvoke(this MethodInfo m, params object[] args)
        {
            return m.Invoke(null, args);
        }

        /// <summary>
        /// 调用静态方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m"></param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static T StaticInvoke<T>(this MethodInfo m, params object[] args)
        {
            return (T)m.StaticInvoke(args);
        }

        /// <summary>
        /// 获取静态方法
        /// </summary>
        /// <param name="t"></param>
        /// <param name="methodName">方法名</param>
        /// <returns></returns>
        public static MethodInfo GetStaticMethod(this Type t, string methodName)
        {
            return t.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        }
    }

    /// <summary>
    /// 指定的可选数据流不存在
    /// </summary>
    public class AlternateDataStreamNotFoundException : Exception
    {
        public AlternateDataStreamNotFoundException(string streamName)
            : base("指定的可选数据流不存在:" + streamName) { }
    }

    /// <summary>
    /// 提供对NTFS操作系统的可选数据流进行操作的工具类
    /// </summary>
    public static class AlternateDataStream
    {
        private static readonly MethodInfo _getStreams;
        private static readonly MethodInfo _createFileStream;
        private static readonly MethodInfo _deleteFileStream;

        /// <summary>
        /// 静态构造器，获取方法
        /// </summary>
        static AlternateDataStream()
        {
            Type t = typeof(AlternateDataStreamUtilities);
            _getStreams = t.GetStaticMethod("GetStreams");
            _createFileStream = t.GetStaticMethod("CreateFileStream");
            _deleteFileStream = t.GetStaticMethod("DeleteFileStream");
        }
        
        /// <summary>
        /// 获取所有的NTFS驱动器
        /// </summary>
        /// <returns></returns>
        public static List<DriveInfo> GetNTFSDrives()
        {
            return DriveInfo.GetDrives().Where(drive => drive.DriveFormat == "NTFS").ToList();
        }

        /// <summary>
        /// 获取文件的所有数据流
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <exception cref="FileNotFoundException"/>
        /// <returns></returns>
        public static List<AlternateStreamData> GetStreamDatas(string file)
        {
            if(File.Exists(file))
                return _getStreams.StaticInvoke<List<AlternateStreamData>>(file);
            else
                throw new FileNotFoundException("文件不存在", file);
        }

        /// <summary>
        /// 确定文件是否存在指定数据流
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <param name="streamName">流名称</param>
        /// <exception cref="FileNotFoundException"/>
        /// <returns></returns>
        public static bool ExistsStream(string file, string streamName)
        {
            return GetStreamDatas(file).Select(asd => asd.Stream).Contains(streamName);
        }

        /// <summary>
        /// 打开指定的文件数据流，如果对应流不存在，则创建一个，用于读写
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <param name="streamName">流名称</param>
        /// <exception cref="FileNotFoundException"/>
        /// <returns></returns>
        public static FileStream OpenOrCreateStream(string file, string streamName)
        {
            if(File.Exists(file))
                return _createFileStream.StaticInvoke<FileStream>(file, streamName,
                    FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            else
                throw new FileNotFoundException("文件不存在", file);
        }

        /// <summary>
        /// 创建指定的文件数据流，如果对应流存在，则覆盖它，仅用于写入数据
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <param name="streamName">流名称</param>
        /// <exception cref="FileNotFoundException"/>
        /// <returns></returns>
        public static FileStream CreateStream(string file, string streamName)
        {
            if(File.Exists(file))
                return _createFileStream.StaticInvoke<FileStream>(file, streamName,
                    FileMode.Create, FileAccess.Write, FileShare.Read);
            else
                throw new FileNotFoundException("文件不存在", file);
        }

        /// <summary>
        /// 删除指定的文件数据流
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <param name="streamName">流名称</param>
        public static void DeleteStream(string file, string streamName)
        {
            _deleteFileStream.StaticInvoke(file, streamName);
        }

        /// <summary>
        /// 读取指定的文件数据流的所有字节，并关闭流
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <param name="streamName">流名称</param>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="AlternateDataStreamNotFoundException"/>
        /// <returns></returns>
        public static byte[] ReadAllBytes(string file, string streamName)
        {
            if(ExistsStream(file, streamName))
            {
                using(FileStream stream = OpenOrCreateStream(file, streamName))
                {
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, (int)stream.Length);
                    return buffer;
                }
            }
            else
                throw new AlternateDataStreamNotFoundException(streamName);
        }

        /// <summary>
        /// 将字节数组写入指定的文件数据流，并关闭流
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <param name="streamName">流名称</param>
        /// <param name="bytes">字节数据</param>
        /// <exception cref="FileNotFoundException"/>
        public static void WriteAllBytes(string file, string streamName, byte[] bytes)
        {
            using(FileStream stream = CreateStream(file, streamName))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        /// <summary>
        /// 读取指定的文件数据流的所有文本，并关闭流
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <param name="streamName">流名称</param>
        /// <param name="encoding">文字编码</param>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="AlternateDataStreamNotFoundException"/>
        /// <returns></returns>
        public static string ReadAllText(string file, string streamName, Encoding encoding)
        {
            if(ExistsStream(file, streamName))
            {
                using(var reader = new StreamReader(OpenOrCreateStream(file, streamName), encoding))
                {
                    return reader.ReadToEnd();
                }
            }
            else
                throw new AlternateDataStreamNotFoundException(streamName);

        }

        /// <summary>
        /// 使用默认编码读取指定的文件数据流的所有文本，并关闭流
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <param name="streamName">流名称</param>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="AlternateDataStreamNotFoundException"/>
        /// <returns></returns>
        public static string ReadAllText(string file, string streamName)
        {
            return ReadAllText(file, streamName, Encoding.Default);
        }

        /// <summary>
        /// 将文本写入指定的文件数据流，并关闭流
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <param name="streamName">流名称</param>
        /// <param name="text">文本</param>
        /// <param name="encoding">编码</param>
        /// <exception cref="FileNotFoundException"/>
        public static void WriteAllText(string file, string streamName, string text, Encoding encoding)
        {
            using(var writer = new StreamWriter(CreateStream(file, streamName), encoding))
            {
                writer.Write(text);
            }
        }

        /// <summary>
        /// 使用默认编码将文本写入指定的文件数据流，并关闭流
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <param name="streamName">流名称</param>
        /// <param name="text">文本</param>
        /// <exception cref="FileNotFoundException"/>
        public static void WriteAllText(string file, string streamName, string text)
        {
            WriteAllText(file, streamName, text, Encoding.Default);
        }

        /// <summary>
        /// 将指定的文件数据流复制到另一个流
        /// </summary>
        /// <param name="file">文件</param>
        /// <param name="streamName">流名称</param>
        /// <param name="stream">保存到的流</param>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="AlternateDataStreamNotFoundException"/>
        public static void CopyToStream(string file, string streamName, Stream stream)
        {
            if(ExistsStream(file, streamName))
            {
                using(var fileStream = OpenOrCreateStream(file, streamName))
                {
                    fileStream.CopyTo(stream);
                }
            }
            else
                throw new AlternateDataStreamNotFoundException(streamName);
        }

        /// <summary>
        /// 将流数据复制到指定的文件数据流，如果流存在则覆盖
        /// </summary>
        /// <param name="file">文件</param>
        /// <param name="streamName">流名称</param>
        /// <param name="stream">要复制的流</param>
        /// <exception cref="FileNotFoundException"/>
        public static void CopyFromStream(string file, string streamName, Stream stream)
        {
            using(var fileStream = CreateStream(file, streamName))
            {
                stream.CopyTo(fileStream);
            }
        }

        /// <summary>
        /// 将指定的文件数据流保存为文件
        /// </summary>
        /// <param name="file">文件</param>
        /// <param name="streamName">流名称</param>
        /// <param name="outputFile">保存到的文件名</param>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="AlternateDataStreamNotFoundException"/>
        public static void SaveStreamToFile(string file, string streamName, string outputFile)
        {
            using(var outStream = File.OpenWrite(outputFile))
            {
                CopyToStream(file, streamName, outStream);
            }
        }

        /// <summary>
        /// 将文件写入指定的数据流
        /// </summary>
        /// <param name="file">文件</param>
        /// <param name="streamName">流名称</param>
        /// <param name="dataFile">要写入的文件</param>
        /// <exception cref="FileNotFoundException"/>
        public static void WriteStreamFromFile(string file, string streamName, string dataFile)
        {
            using(var dataStream = File.OpenRead(dataFile))
            {
                CopyFromStream(file, streamName, dataStream);
            }
        }

    }
}
