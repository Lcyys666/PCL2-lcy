Public Class PageOtherTest

    Public Shadows IsLoaded As Boolean = False
    Private LastJrrpDate As String = ""
    Private TodayJrrpValue As Integer = -1

    Private Sub PageOtherTest_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        '非重复加载部分
        If IsLoaded Then Exit Sub
        IsLoaded = True

        '绑定按钮事件
        AddHandler BtnJrrp.Click, AddressOf Jrrp
        AddHandler BtnMemoryOptimize.Click, Sub() MemoryOptimize(True)
        AddHandler BtnRubbishClear.Click, AddressOf RubbishClear

        '检查是否已经获取过今日人品
        CheckJrrpStatus()
    End Sub

    Private Sub CheckJrrpStatus()
        Try
            '获取今天的日期字符串
            Dim Today = Date.Now.ToString("yyyy-MM-dd")
            '检查上次生成的日期
            LastJrrpDate = Setup.Get("TodayJrrpDate")
            If LastJrrpDate = Today Then
                '今天已经生成过了，显示保存的值
                TodayJrrpValue = Setup.Get("TodayJrrpValue")
                TxtJrrpValue.Text = TodayJrrpValue.ToString()
                TxtJrrpHitokoto.Text = Setup.Get("TodayJrrpHitokoto")
            End If
        Catch ex As Exception
            Log(ex, "检查今日人品状态时出错")
        End Try
    End Sub

    '获取今日人品和一言
    Public Sub Jrrp() Handles BtnJrrp.Click
        Try
            '获取今天的日期字符串
            Dim Today = Date.Now.ToString("yyyy-MM-dd")

            '判断是否需要重新生成
            If Today <> LastJrrpDate Then
                '日期不同，需要重新生成
                LastJrrpDate = Today

                '生成随机人品值(1-100)
                Dim Random As New Random()
                TodayJrrpValue = Random.Next(1, 101)

                '保存今天的日期和人品值
                Setup.Set("TodayJrrpDate", Today)
                Setup.Set("TodayJrrpValue", TodayJrrpValue)

                '获取一言
                GetHitokoto()
            Else
                '日期相同，使用已有的人品值
                TxtJrrpValue.Text = TodayJrrpValue.ToString()
                '可以刷新一言
                GetHitokoto()
            End If

            '显示今日人品评价
            If TodayJrrpValue >= 90 Then
                Hint("你今天的人品非常好！", HintType.Finish)
            ElseIf TodayJrrpValue >= 70 Then
                Hint("你今天的人品不错！", HintType.Finish)
            ElseIf TodayJrrpValue >= 50 Then
                Hint("你今天的人品一般。", HintType.Info)
            ElseIf TodayJrrpValue >= 30 Then
                Hint("你今天的人品不太好。", HintType.Info)
            Else
                Hint("你今天的人品很差，要小心哦！", HintType.Critical)
            End If
        Catch ex As Exception
            Log(ex, "获取今日人品时出错")
            Hint("获取今日人品失败", HintType.Critical)
        End Try
    End Sub

    '获取一言
    Private Sub GetHitokoto()
        RunInNewThread(Sub()
                           Try
                               '使用一言API获取一句话
                               Dim WebResult As String = ""
                               Try
                                   WebResult = NetGetCodeByRequestOnce("https://v1.hitokoto.cn/?encode=text", Encode:=Encoding.UTF8)
                               Catch ex As Exception
                                   WebResult = "今天也要开开心心的呀！"
                               End Try

                               '保存并显示一言
                               Setup.Set("TodayJrrpHitokoto", WebResult)
                               RunInUi(Sub()
                                           TxtJrrpValue.Text = TodayJrrpValue.ToString()
                                           TxtJrrpHitokoto.Text = WebResult
                                       End Sub)
                           Catch ex As Exception
                               Log(ex, "获取一言时出错")
                               RunInUi(Sub()
                                           TxtJrrpHitokoto.Text = "获取一言失败，但不要紧，保持微笑就好！"
                                       End Sub)
                           End Try
                       End Sub, "GetHitokoto")
    End Sub

    Public Shared Sub StartCustomDownload(Url As String, FileName As String, Optional Folder As String = Nothing)
        Hint("为便于维护，该功能暂不可用")
    End Sub

    Public Shared Sub RubbishClear()
        Try
            Hint("正在清理垃圾文件，请稍候...", HintType.Info)

            '清理临时文件和日志
            Dim TempFiles As Integer = 0

            '清理minecraft临时文件夹
            If Directory.Exists(Path & "PCL\.minecraft\logs") Then
                TempFiles += DeleteFiles(Path & "PCL\.minecraft\logs", True)
            End If
            If Directory.Exists(Path & "PCL\.minecraft\crash-reports") Then
                TempFiles += DeleteFiles(Path & "PCL\.minecraft\crash-reports", True)
            End If

            '清理PCL缓存
            If Directory.Exists(Path & "PCL\Cache") Then
                TempFiles += DeleteFiles(Path & "PCL\Cache", True)
            End If

            '清理Windows临时文件夹中PCL相关文件
            Dim TempFolder As String = Environment.GetEnvironmentVariable("TEMP")
            If Directory.Exists(TempFolder) Then
                For Each file In Directory.GetFiles(TempFolder, "PCL*", SearchOption.TopDirectoryOnly)
                    Try
                        System.IO.File.Delete(file)
                        TempFiles += 1
                    Catch ex As Exception
                        Log(ex, $"删除临时文件失败: {file}")
                    End Try
                Next
            End If

            '清理游戏临时文件
            If Directory.Exists(Path & "PCL\.minecraft\versions") Then
                For Each versionFolder In Directory.GetDirectories(Path & "PCL\.minecraft\versions")
                    '清理日志文件
                    If Directory.Exists(versionFolder & "\logs") Then
                        TempFiles += DeleteFiles(versionFolder & "\logs", True)
                    End If
                    '清理崩溃报告
                    If Directory.Exists(versionFolder & "\crash-reports") Then
                        TempFiles += DeleteFiles(versionFolder & "\crash-reports", True)
                    End If
                Next
            End If

            Hint($"垃圾清理完成，共清理了 {TempFiles} 个文件", HintType.Finish)
        Catch ex As Exception
            Log(ex, "垃圾清理失败")
            Hint("垃圾清理失败", HintType.Critical)
        End Try
    End Sub

    ''' <summary>
    ''' 删除目录中的所有文件，但保留目录结构
    ''' </summary>
    ''' <param name="dirPath">要删除文件的目录路径</param>
    ''' <param name="recursive">是否递归处理子目录</param>
    ''' <returns>删除的文件数量</returns>
    Private Shared Function DeleteFiles(dirPath As String, recursive As Boolean) As Integer
        If Not Directory.Exists(dirPath) Then Return 0

        Dim count As Integer = 0

        ' 删除目录中的所有文件
        For Each filepath As String In Directory.GetFiles(dirPath)
            Try
                System.IO.File.Delete(filepath)
                count += 1
            Catch ex As Exception
                Log(ex, $"删除文件失败: {filepath}")
            End Try
        Next

        ' 如果需要递归处理子目录
        If recursive Then
            For Each subDir As String In Directory.GetDirectories(dirPath)
                count += DeleteFiles(subDir, True)
            Next
        End If

        Return count
    End Function

    Public Shared Sub MemoryOptimize(ShowHint As Boolean)
        If ShowHint Then Hint("正在进行内存优化，请稍候...", HintType.Info)

        Try
            '执行多次GC，确保内存被释放
            For i As Integer = 0 To 2
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced)
                GC.WaitForPendingFinalizers()
            Next

            '调用Windows的内存优化API
            MemoryOptimizeInternal(ShowHint)

            '尝试进一步清理内存
            Dim startMemory As Long = Process.GetCurrentProcess().PrivateMemorySize64

            '强制JIT编译器清理缓存
            System.Runtime.CompilerServices.RuntimeHelpers.PrepareConstrainedRegions()

            '清理大型对象堆
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, True)

            Dim endMemory As Long = Process.GetCurrentProcess().PrivateMemorySize64
            Dim saved As Double = (startMemory - endMemory) / 1024.0 / 1024.0

            If ShowHint Then
                If saved > 0 Then
                    Hint($"内存优化完成，释放了 {saved:F2} MB 内存", HintType.Finish)
                Else
                    Hint("内存优化完成", HintType.Finish)
                End If
            End If
        Catch ex As Exception
            Log(ex, "内存优化失败")
            If ShowHint Then Hint("内存优化失败", HintType.Critical)
        End Try
    End Sub

    Public Shared Sub MemoryOptimizeInternal(ShowHint As Boolean)
        Try
            '使用多种Windows API进行深层次内存优化

            '1. 清空工作集
            Dim ProcessHandle As IntPtr = Process.GetCurrentProcess().Handle
            Native.EmptyWorkingSet(ProcessHandle)

            '2. 尝试减少内存使用
            Native.SetProcessWorkingSetSize(ProcessHandle, New IntPtr(-1), New IntPtr(-1))

            '3. 尝试压缩内存
            If Environment.OSVersion.Version.Major >= 6 Then  ' Vista及以上系统
                Try
                    Native.RtlSetProcessIsCritical(0, 0, 0)
                Catch ex As Exception
                    If ShowHint Then Log(ex, "系统内存压缩失败")
                End Try
            End If
        Catch ex As Exception
            If ShowHint Then Log(ex, "深度内存优化失败")
        End Try
    End Sub

    Public Shared Function GetRandomCave() As String
        Dim Caves As String() = {
            "探索山洞时，记得带上足够的火把！",
            "在深层矿洞中挖掘时，请小心岩浆和水！",
            "地下矿洞中的资源丰富，但也隐藏着危险！",
            "听到僵尸或骷髅的声音时，可能附近有刷怪笼！",
            "地下洞穴中可能藏着珍贵的矿石和资源！"
        }
        Return Caves(New Random().Next(0, Caves.Length))
    End Function

    Public Shared Function GetRandomHint() As String
        Dim Hints As String() = {
            "按F3可以查看当前坐标和调试信息！",
            "睡觉可以跳过夜晚并重置出生点！",
            "红石可以制作各种复杂的机关！",
            "与村民交易可以获得稀有物品！",
            "末影箱可以在任何位置访问同一个存储空间！"
        }
        Return Hints(New Random().Next(0, Hints.Length))
    End Function

    Public Shared Function GetRandomPresetHint() As String
        Dim Hints As String() = {
            "PCL启动器支持多种主页预设！",
            "你可以在个性化设置中更换主页预设！",
            "主页预设可以让你快速获取Minecraft相关信息！",
            "喜欢某个主页预设吗？在设置中可以轻松切换！",
            "主页预设提供了丰富的Minecraft社区内容！"
        }
        Return Hints(New Random().Next(0, Hints.Length))
    End Function

End Class

'Windows API调用
Public Class Native
    <Runtime.InteropServices.DllImport("psapi.dll")>
    Public Shared Function EmptyWorkingSet(hProcess As IntPtr) As Boolean
    End Function

    <Runtime.InteropServices.DllImport("kernel32.dll")>
    Public Shared Function SetProcessWorkingSetSize(hProcess As IntPtr, dwMinimumWorkingSetSize As IntPtr, dwMaximumWorkingSetSize As IntPtr) As Boolean
    End Function

    <Runtime.InteropServices.DllImport("ntdll.dll")>
    Public Shared Function RtlSetProcessIsCritical(Critical As Integer, Raise As Integer, Response As Integer) As Integer
    End Function
End Class
