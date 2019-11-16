Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.InteropServices
Imports AutoItX3Lib

Public Class Main
    '20190929 created
    '目的有: 1. 監控問診畫面, 紀錄 -done
    '        2. 顯示該人的檢驗或其他有用資料  -done
    '        3. 可貼上routine template -plausible
    '        4. 可貼上檢驗結果 -plausible
    '        5. 可copy 雲端    -done
    '        6. 可紀錄是否有查雲端, 是否有查關懷名單 -done
    '        7. 量表 -plausible
    ' 1.0.0.3 20191004 add current_uid.txt
    ' 1.0.0.4 20191006 可以看到雲端藥歷, 還有檢驗; Hot key: 日期+OK
    ' 1.0.0.9 20191015 有名字,有顏色
    ' 1.0.0.10 20191019 有診斷
    ' 1.0.0.11 20191021 加了中醫
    ' 1.0.0.18 20191025 增加過敏, 牙醫
    ' 20191025: 刪掉[抓到了], page load refresh_data
    Private aut As New AutoItX3
    Private strID As String = ""
    Private strUID As String = ""
    Private strSDATE As String = ""
    Private tempID As String = ""
    Public Const MOD_ALT As Integer = &H1 'Alt key for hotkey
    Public Const MOD_SHIFT As Integer = &H4 'Alt key for hotkey
    Public Const MOD_CONTROL As Integer = &H2 'Alt key for hotkey
    Public Const MOD_WINKEY As Integer = &H8 'Alt key for hotkey
    Public Const WM_HOTKEY As Integer = &H312   'Hotkey

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        '20190929 created
        '定義tempID
        '判斷windows 是否存在?
        If aut.WinExists("問診畫面") Then
            Try
                tempID = aut.WinGetText("問診畫面").Split(vbLf)(2)
            Catch
                tempID = ""
            End Try
        Else
            tempID = ""
        End If
        If strID = "" Then
            If tempID = "" Then
                ' condition 1, strID = "" => ""                     do nothing
                Exit Sub
            Else
                ' 檢單查核, 如果分解後數目小於8, 應該就不是正確的
                ' 20190930似乎有效
                If tempID.Split(" ").Count < 8 Then
                    'MessageBox.Show("抓到了")
                    Exit Sub
                End If
                ' condition 2, strID = "" => something A            record A, starttime
                ' 要做很多事情, 分解
                ' 20190930 有些"問診畫面"的狀態,文字是不一樣的,這樣的話會有錯誤
                strID = tempID
                Dim dc As New ComDataDataContext
                Dim s() As String = strID.Split(" ")   '0 SDATE, 1 VIST, 2 RMNO, 4 Nr, 7 uid, 8 cname
                strUID = s(7).Substring(1, 10)
                If strUID = "A000000000" Then
                    strID = ""
                    strUID = ""
                    strSDATE = ""
                    Exit Sub
                End If
                strSDATE = s(0)
                dc.sp_insert_access(CDate(s(0)), s(1), CInt(s(2)), CInt(s(4)), strUID, s(8), 1)
                ' do something
                Me.Label1.Text = strUID
                Me.Label2.Text = strID
                ' 寫入current_uid
                If System.IO.File.Exists("C:\vpn\current_uid.txt") Then
                    ' 如果有檔案就殺了它
                    System.IO.File.Delete("C:\vpn\current_uid.txt")
                End If
                Dim sw As New System.IO.StreamWriter("C:\vpn\current_uid.txt")
                sw.Write(strUID)
                sw.Close()
                ' 更新資料
                Refresh_data()
            End If
        Else
            If strID = tempID Then
                ' condition 3, strID = something A => something A   do nothing
                Exit Sub
            ElseIf tempID = "" Then
                ' condition 4, strID = something A => ""            record endtime, write into database
                ' 做的事情也不少
                Dim dc As New ComDataDataContext
                Dim s() As String = strID.Split(" ")   '0 SDATE, 1 VIST, 2 RMNO, 4 Nr, 7 uid, 8 cname
                dc.sp_insert_access(CDate(s(0)), s(1), CInt(s(2)), CInt(s(4)), s(7).Substring(1, 10), s(8), 0)
                strID = tempID
                strUID = ""
                strSDATE = ""
                Me.Label1.Text = ""
                Me.Label2.Text = ""
                If System.IO.File.Exists("C:\vpn\current_uid.txt") Then
                    ' 如果有檔案就殺了它
                    System.IO.File.Delete("C:\vpn\current_uid.txt")
                End If
                ' 清理檢驗資料
                Me.DataGridView1.Visible = False
                Me.DataGridView2.Visible = False
                Me.DataGridView3.Visible = False
                Me.DataGridView4.Visible = False
                Me.DataGridView5.Visible = False
                Me.DataGridView6.Visible = False
                Me.DataGridView7.Visible = False
                Me.DataGridView8.Visible = False
            Else
                ' condition 5, strID = something A => something B   I don't know if this is possible
                ' 有可能嗎? 我不知道
                ' 20191001 答案揭曉了,有可能,因為THESIS在畫form時會有A000000000臨時的資料,然後再讀資料庫蓋上,就會出現something A => something B的情況
                ' 我採用檢核若A000000000的情形就不要寫入的方式處理
                ' 檢單查核, 如果分解後數目小於8, 應該就不是正確的
                '                MessageBox.Show("抓到了! " + vbCrLf + strID + "=>" + tempID)
                If tempID.Split(" ").Count < 8 Then
                    Exit Sub
                End If
            End If
        End If
    End Sub

    <DllImport("User32.dll")>
    Public Shared Function RegisterHotKey(ByVal hwnd As IntPtr,
                        ByVal id As Integer, ByVal fsModifiers As Integer,
                        ByVal vk As Integer) As Integer
    End Function

    <DllImport("User32.dll")>
    Public Shared Function UnregisterHotKey(ByVal hwnd As IntPtr,
                        ByVal id As Integer) As Integer
    End Function

    Private Sub Form1_Load(ByVal sender As System.Object,
                        ByVal e As System.EventArgs) Handles MyBase.Load
        RegisterHotKey(Me.Handle, 100, MOD_ALT, Keys.D)
        RegisterHotKey(Me.Handle, 200, MOD_ALT, Keys.C)
        Record_adm("Companion Log in", "")
        Refresh_data()
        Me.Label1.Text = ""
        Me.Label2.Text = ""

    End Sub

    Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
        If m.Msg = WM_HOTKEY Then
            Dim id As IntPtr = m.WParam
            Select Case (id.ToString)
                Case "100"
                    MessageBox.Show("You pressed ALT+D key combination")
                Case "200"
                    'MessageBox.Show("You pressed ALT+C key combination")
                    Dim strAnswer As List(Of String) = {"OK.", "Stationary condition.", "For drug refill.", "No specific complaints.", "No change in clinical picture.",
                                                  "Satisfied with medication.", "Improved condition.", "Stable mental status.", "Maintenance phase.", "Nothing particular."}.ToList
                    ' 先決定一句還是兩句
                    Dim n As Int16 = CInt(Math.Ceiling(Rnd() * 2))
                    Dim chosen As Int16 = CInt(Math.Ceiling(Rnd() * 10)) - 1
                    Dim output As String = strAnswer.Item(chosen)
                    If n = 2 Then
                        strAnswer.Remove(output)
                        output += " " + strAnswer.Item(CInt(Math.Ceiling(Rnd() * 9)) - 1)
                    End If
                    If strSDATE = "" Then
                        output = Now.ToShortDateString + ": " + output + vbCr
                    Else
                        output = strSDATE + ": " + output + vbCr
                    End If
                    SendKeys.Send(output)
            End Select
        End If
        MyBase.WndProc(m)
    End Sub

    Private Sub Main_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        UnregisterHotKey(Me.Handle, 100)
        UnregisterHotKey(Me.Handle, 200)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Me.RichTextBox1.AppendText(strUID + "; ")
        '        Dim strS = "{\rtf1\cpg950 Hello!\par This is some {\b \i bold " + strID + "} text.\par}"
        Dim strS = "{\rtf1\ansi\deff0{\fonttbl{\f0\fnil\fcharset136 \'b7\'73\'b2\'d3\'a9\'fa\'c5\'e9;}{\f1\fswiss\fcharset0 Arial;}} \viewkind4\uc1\pard\lang1028\f0\fs28\'ad\'ec\'a5\'bb\'a6\'62\'b9\'fc\'a4\'c6\'a6\'77\'b8\'6d\'be\'f7\'ba\'63, \f1 since last year\f0\'a5\'68\'a6\'7e\'a5\'7c\'a4\'eb\par \'ae\'61\'a6\'ed\'c5\'61\'ba\'71, \'b3\'df\'c4\'40\par \f1 parents, younger brother\f0 , \f1 mother\f0\'ad\'ab\'ab\'d7, \f1 father\f0\'a4\'a4\'ab\'d7, \f1 younger brother in USA, \f0\'bb\'b4\'ab\'d7\par \f1\fs29 he lived in USA also 10+ years\par \f0\fs28\'b0\'ea\'a4\'70\'b2\'a6\'b7\'7e, \'c5\'61\'ba\'71\'b0\'ea\'a4\'70\par \'a5\'69\'c5\'aa\'b0\'ea\'a6\'72, \'a6\'62\'ac\'fc\'b0\'ea\'b6\'7d\'c0\'5c\'c6\'55\b\i\fs18\par \par}"
        Dim st As New MemoryStream
        Dim stWr As New StreamWriter(st)
        stWr.Write(strS)
        stWr.Flush()
        st.Position = 0
        Me.RichTextBox1.LoadFile(st, RichTextBoxStreamType.RichText)
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        'Clipboard.SetText(Me.RichTextBox1.Rtf)
        'Clipboard.GetText(TextDataFormat.Rtf)
        Refresh_data()
    End Sub

    Private Sub Refresh_data()
        Dim b1 As Boolean = False
        Dim b2 As Boolean = False
        Dim b5 As Boolean = False
        Dim b6 As Boolean = False
        Dim b7 As Boolean = False
        For Each t As TabPage In Me.TabControl1.TabPages
            Select Case t.Text
                Case "檢驗結果"
                    b1 = True
                Case "雲端資料"
                    b2 = True
                Case "手術牙醫"
                    b6 = True
                Case "中醫復健"
                    b5 = True
                Case "關懷出院過敏"
                    b7 = True
                Case Else
            End Select
        Next
        If Not b1 Then
            Me.TabControl1.TabPages.Insert(0, TabPage1)
        End If
        If Not b2 Then
            Me.TabControl1.TabPages.Insert(1, TabPage2)
        End If
        If Not b6 Then
            Me.TabControl1.TabPages.Insert(2, TabPage6)
        End If
        If Not b5 Then
            Me.TabControl1.TabPages.Insert(3, TabPage5)
        End If
        If Not b7 Then
            Me.TabControl1.TabPages.Insert(4, TabPage7)
        End If
        Dim dc As New ComDataDataContext
        Me.DataGridView9.DataSource = dc.sp_querytable
        If strUID = "" Then
            Exit Sub
        End If
        ' 讀入檢驗資料 with strUID
        Dim dgv_lab As New List(Of DataGridView)
        Me.DataGridView1.DataSource = dc.sp_labdata_DRUG_by_uid(strUID)
        Me.DataGridView2.DataSource = dc.sp_labdata_hepa_by_uid(strUID)
        Me.DataGridView3.DataSource = dc.sp_labdata_DM_by_uid(strUID)
        Me.DataGridView4.DataSource = dc.sp_labdata_CBC_by_uid(strUID)
        Me.DataGridView5.DataSource = dc.sp_labdata_UA_by_uid(strUID)
        Me.DataGridView6.DataSource = dc.sp_labdata_OTHER_by_uid(strUID)
        Me.DataGridView7.DataSource = dc.sp_cloudlab_by_uid(strUID)
        Me.DataGridView8.DataSource = dc.sp_cloudmed_by_uid(strUID)
        Me.DataGridView10.DataSource = dc.sp_cloudOP_by_uid(strUID)
        Me.DataGridView11.DataSource = dc.sp_cloudDEN_by_uid(strUID)
        Me.DataGridView12.DataSource = dc.sp_cloudREH_by_uid(strUID)
        Me.DataGridView13.DataSource = dc.sp_cloudTCM_by_uid(strUID)
        Me.DataGridView14.DataSource = dc.sp_cloudDIS_by_uid(strUID)
        Me.DataGridView15.DataSource = dc.sp_cloudSCH_R_by_uid(strUID)
        Me.DataGridView16.DataSource = dc.sp_cloudSCH_U_by_uid(strUID)
        Me.DataGridView17.DataSource = dc.sp_cloudALL_by_uid(strUID)
        If Me.DataGridView1.Rows.Count = 0 Then
            Me.DataGridView1.Visible = False
        Else
            Me.DataGridView1.Visible = True
            dgv_lab.Add(Me.DataGridView1)
        End If
        If Me.DataGridView2.Rows.Count = 0 Then
            Me.DataGridView2.Visible = False
        Else
            Me.DataGridView2.Visible = True
            dgv_lab.Add(Me.DataGridView2)
        End If
        If Me.DataGridView3.Rows.Count = 0 Then
            Me.DataGridView3.Visible = False
        Else
            Me.DataGridView3.Visible = True
            dgv_lab.Add(Me.DataGridView3)
        End If
        If Me.DataGridView4.Rows.Count = 0 Then
            Me.DataGridView4.Visible = False
        Else
            Me.DataGridView4.Visible = True
            dgv_lab.Add(Me.DataGridView4)
        End If
        If Me.DataGridView5.Rows.Count = 0 Then
            Me.DataGridView5.Visible = False
        Else
            Me.DataGridView5.Visible = True
            dgv_lab.Add(Me.DataGridView5)
        End If
        If Me.DataGridView6.Rows.Count = 0 Then
            Me.DataGridView6.Visible = False
        Else
            Me.DataGridView6.Visible = True
            dgv_lab.Add(Me.DataGridView6)
        End If
        Select Case dgv_lab.Count
            Case 0
                Me.TabControl1.TabPages.Remove(TabPage1)
            Case 1
                dgv_lab(0).Size = New Size(776, 487)
                dgv_lab(0).Location = New Point(0, 3)
            Case 2
                dgv_lab(0).Size = New Size(776, 240)
                dgv_lab(1).Size = New Size(776, 240)
                dgv_lab(0).Location = New Point(0, 3)
                dgv_lab(1).Location = New Point(0, 250)
            Case 3
                dgv_lab(0).Size = New Size(776, 160)
                dgv_lab(1).Size = New Size(776, 160)
                dgv_lab(2).Size = New Size(776, 160)
                dgv_lab(0).Location = New Point(0, 3)
                dgv_lab(1).Location = New Point(0, 166)
                dgv_lab(2).Location = New Point(0, 331)
            Case 4
                dgv_lab(0).Size = New Size(776, 160)
                dgv_lab(1).Size = New Size(776, 160)
                dgv_lab(2).Size = New Size(385, 160)
                dgv_lab(3).Size = New Size(385, 160)
                dgv_lab(0).Location = New Point(0, 3)
                dgv_lab(1).Location = New Point(0, 166)
                dgv_lab(2).Location = New Point(0, 331)
                dgv_lab(3).Location = New Point(390, 331)
            Case 5
                dgv_lab(0).Size = New Size(776, 160)
                dgv_lab(1).Size = New Size(385, 160)
                dgv_lab(2).Size = New Size(385, 160)
                dgv_lab(3).Size = New Size(385, 160)
                dgv_lab(4).Size = New Size(385, 160)
                dgv_lab(0).Location = New Point(0, 3)
                dgv_lab(1).Location = New Point(0, 166)
                dgv_lab(2).Location = New Point(0, 331)
                dgv_lab(3).Location = New Point(390, 166)
                dgv_lab(4).Location = New Point(390, 331)
            Case 6
                dgv_lab(0).Size = New Size(385, 160)
                dgv_lab(1).Size = New Size(385, 160)
                dgv_lab(2).Size = New Size(385, 160)
                dgv_lab(3).Size = New Size(385, 160)
                dgv_lab(4).Size = New Size(385, 160)
                dgv_lab(5).Size = New Size(385, 160)
                dgv_lab(0).Location = New Point(0, 3)
                dgv_lab(1).Location = New Point(0, 166)
                dgv_lab(2).Location = New Point(0, 331)
                dgv_lab(3).Location = New Point(390, 3)
                dgv_lab(4).Location = New Point(390, 166)
                dgv_lab(5).Location = New Point(390, 331)
            Case Else
        End Select
        ' dgv7,8 屬於雲端藥歷與檢驗
        If Me.DataGridView7.Rows.Count = 0 Then
            Me.DataGridView7.Visible = False
            If Me.DataGridView8.Rows.Count = 0 Then
                Me.TabControl1.TabPages.Remove(TabPage2)
                Me.DataGridView8.Visible = False
                '兩個都沒有
            Else
                '有dgv8, 沒有dgv7
                Me.DataGridView8.Location = New Point(3, 3)
                Me.DataGridView8.Size = New Size(770, 487)
                Me.DataGridView8.Visible = True
            End If
        Else
            Me.DataGridView7.Visible = True
            Me.DataGridView7.Location = New Point(3, 3)
            If Me.DataGridView8.Rows.Count = 0 Then
                '有dgv7, 沒有dgv8
                Me.DataGridView7.Size = New Size(770, 487)
                Me.DataGridView8.Visible = False
            Else
                '兩個都有
                Me.DataGridView7.Size = New Size(770, 240)
                Me.DataGridView8.Size = New Size(770, 240)
                Me.DataGridView8.Location = New Point(3, 250)
                Me.DataGridView8.Visible = True
            End If
        End If
        ' dgv10,11 屬於手術與牙醫
        If Me.DataGridView10.Rows.Count = 0 Then
            Me.DataGridView10.Visible = False
            If Me.DataGridView11.Rows.Count = 0 Then
                Me.DataGridView11.Visible = False
                '兩個都沒有
                Me.TabControl1.TabPages.Remove(TabPage6)
            Else
                '有dgv11, 沒有dgv10
                Me.DataGridView11.Location = New Point(3, 3)
                Me.DataGridView11.Size = New Size(770, 487)
                Me.DataGridView11.Visible = True
            End If
        Else
            Me.DataGridView10.Visible = True
            Me.DataGridView10.Location = New Point(3, 3)
            If Me.DataGridView11.Rows.Count = 0 Then
                '有dgv10, 沒有dgv11
                Me.DataGridView10.Size = New Size(770, 487)
                Me.DataGridView11.Visible = False
            Else
                '兩個都有
                Me.DataGridView10.Size = New Size(770, 240)
                Me.DataGridView11.Size = New Size(770, 240)
                Me.DataGridView11.Location = New Point(3, 250)
                Me.DataGridView11.Visible = True
            End If
        End If
        ' dgv12,13 屬於中醫與復健
        If Me.DataGridView12.Rows.Count = 0 Then
            Me.DataGridView12.Visible = False
            If Me.DataGridView13.Rows.Count = 0 Then
                Me.DataGridView13.Visible = False
                '兩個都沒有
                Me.TabControl1.TabPages.Remove(TabPage5)
            Else
                '有dgv13, 沒有dgv12
                Me.DataGridView13.Location = New Point(3, 3)
                Me.DataGridView13.Size = New Size(770, 487)
                Me.DataGridView13.Visible = True
            End If
        Else
            Me.DataGridView12.Visible = True
            Me.DataGridView12.Location = New Point(3, 3)
            If Me.DataGridView13.Rows.Count = 0 Then
                '有dgv12, 沒有dgv13
                Me.DataGridView13.Visible = False
            Else
                '兩個都有
                Me.DataGridView12.Size = New Size(770, 240)
                Me.DataGridView13.Size = New Size(770, 240)
                Me.DataGridView13.Location = New Point(3, 250)
                Me.DataGridView13.Visible = True
            End If
        End If
        ' dgv 14, 15, 16, 17是出院病摘, 過敏, 還有關懷名單
        Dim dgv_sch As New List(Of DataGridView)
        If Me.DataGridView14.Rows.Count = 0 Then
            Me.DataGridView14.Visible = False
        Else
            Me.DataGridView14.Visible = True
            dgv_sch.Add(Me.DataGridView14)
        End If
        If Me.DataGridView15.Rows.Count = 0 Then
            Me.DataGridView15.Visible = False
        Else
            Me.DataGridView15.Visible = True
            dgv_sch.Add(Me.DataGridView15)
        End If
        If Me.DataGridView16.Rows.Count = 0 Then
            Me.DataGridView16.Visible = False
        Else
            Me.DataGridView16.Visible = True
            dgv_sch.Add(Me.DataGridView16)
        End If
        If Me.DataGridView17.Rows.Count = 0 Then
            Me.DataGridView17.Visible = False
        Else
            Me.DataGridView17.Visible = True
            dgv_sch.Add(Me.DataGridView17)
        End If
        Select Case dgv_sch.Count
            Case 0
                Me.TabControl1.TabPages.Remove(TabPage7)
            Case 1
                dgv_sch(0).Size = New Size(776, 487)
                dgv_sch(0).Location = New Point(0, 3)
            Case 2
                dgv_sch(0).Size = New Size(776, 240)
                dgv_sch(1).Size = New Size(776, 240)
                dgv_sch(0).Location = New Point(0, 3)
                dgv_sch(1).Location = New Point(0, 250)
            Case 3
                dgv_sch(0).Size = New Size(776, 160)
                dgv_sch(1).Size = New Size(776, 160)
                dgv_sch(2).Size = New Size(776, 160)
                dgv_sch(0).Location = New Point(0, 3)
                dgv_sch(1).Location = New Point(0, 166)
                dgv_sch(2).Location = New Point(0, 331)
            Case 4
                dgv_sch(0).Size = New Size(385, 240)
                dgv_sch(1).Size = New Size(385, 240)
                dgv_sch(2).Size = New Size(385, 240)
                dgv_sch(3).Size = New Size(385, 240)
                dgv_sch(0).Location = New Point(0, 3)
                dgv_sch(1).Location = New Point(0, 250)
                dgv_sch(2).Location = New Point(390, 3)
                dgv_sch(3).Location = New Point(390, 250)
            Case Else
        End Select
    End Sub

    Private Sub Main_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        Record_adm("Companion Log out", "")
    End Sub

    Private Sub Datagridviw_cellformatting(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DataGridViewCellFormattingEventArgs) Handles DataGridView9.CellFormatting
        If e.Value Is Nothing Then
            Exit Sub
        End If
        If e.Value.GetType Is GetType(Boolean) Then
            If e.Value = True Then
                DataGridView9.Rows(e.RowIndex).Cells(e.ColumnIndex).Style.ForeColor = Color.Red
            Else
                DataGridView9.Rows(e.RowIndex).Cells(e.ColumnIndex).Style.ForeColor = Color.Black
            End If
        End If
    End Sub

End Class
