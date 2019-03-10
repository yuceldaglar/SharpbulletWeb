<%@ Page Language="C#" MasterPageFile="~/Assets/Empty.master" PublicPage="true" Inherits="SharpBullet.Web.SbPage" %>
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
                    new SbPasswordEditor() { Text = LibraryText.Instance.Password(), DataSource="Model", DataField = "Password" },
                    new SbPasswordEditor() { Text = LibraryText.Instance.Password2(), DataSource="Model", DataField = "Password2" }
                )
        ),
        new SbRow().Add(
            new SbColumn() { ColSize = 4 },
            new SbColumn() { ColSize = 4 }
                .Add(new SbButton() { Text = LibraryText.Instance.SignUp(), Click = "doSignUp()" })       
        )
    ).Add("doSignUp", @"
                $scope.doSignUp = function () {

                    var data = JSON.stringify($scope.Model);
                    $.ajax({
                        url: api('SignUp.Create'),
                        type: 'post',
                        data: data,
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