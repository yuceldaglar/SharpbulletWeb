<%@ Page Language="C#" MasterPageFile="~/Assets/Site.master" %>
<%@ Import Namespace="SharpBullet.Web.Controls.Editors" %>
<%@ Import Namespace="SharpBullet.Web.Controls" %>
<%@ Import Namespace="System.Web.Configuration" %>
<%@ Import Namespace="App.Library" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
    <style>
        html {
            background-color: #f2f2f2;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="content" Runat="Server">
<%= new SbForm()
     .Add(
        new SbRow().Add(
            new SbColumn() { ColSize = 4 },
            new SbColumn() { ColSize = 4 }
                .Add(
                    new SbTextEditor() { Text = LibraryText.Instance.Email(), DataSource="Model", DataField = "Email" },
                    new SbPasswordEditor() { Text = LibraryText.Instance.Password(), DataSource="Model", DataField = "Password" }
                )
        ),
        new SbRow().Add(
            new SbColumn() { ColSize = 4 },
            new SbColumn() { ColSize = 4 }
                .Add(new SbButton() { Text = LibraryText.Instance.Login(), Click = "doLogin()" })       
        )
    ).Add("doLogin", @"
                $scope.doLogin = function () {

                    var json = JSON.stringify($scope.Model);
                    $.ajax({
                        url: api('Login.Create'),
                        type: 'post',
                        data: json,
                        success: function (message) {
                            if(!message || !message.Value) return;
                            window.location = message.Value;
                        }
                    });
                }
    "
    ).Render()
%>
</asp:Content>