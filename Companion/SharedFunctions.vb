Imports System.Net

Module SharedFunctions

    Public Sub Record_error(ByVal er As String)
        '寫入錯誤訊息
        Dim dc As New ComDataContext
        Dim newErr As New log_Err With {
            .error_date = Now,
            .application_name = My.Application.Info.ProductName + " V" + My.Application.Info.Version.ToString,
            .machine_name = Dns.GetHostName,
            .ip_address = Dns.GetHostEntry(Dns.GetHostName).AddressList(0).ToString(),
            .userid = "Darren",
            .error_message = er
        }
        dc.log_Err.InsertOnSubmit(newErr)
        dc.SubmitChanges()
    End Sub

    Public Sub Record_adm(ByVal op As String, ByVal des As String)
        '寫入作業訊息
        Dim dc As New ComDataContext
        Dim newLog As New log_Adm With {
            .regdate = Now,
            .application_name = My.Application.Info.ProductName + " V" + My.Application.Info.Version.ToString,
            .machine_name = Dns.GetHostName,
            .ip_address = Dns.GetHostEntry(Dns.GetHostName).AddressList(0).ToString(),
            .userid = "Darren",
            .operation_name = op,
            .description = des
        }
        dc.log_Adm.InsertOnSubmit(newLog)
        dc.SubmitChanges()
    End Sub


End Module
