﻿using System;
using System.Windows;
using System.Windows.Input;
using KeePass.Sources.DropBox.Api;
using KeePass.Utils;
using Microsoft.Phone.Shell;

namespace KeePass.Sources.DropBox
{
    public partial class DropBox
    {
        private readonly ApplicationBarIconButton _cmdAppBarOpen;

        public DropBox()
        {
            InitializeComponent();

            _cmdAppBarOpen = (ApplicationBarIconButton)
                ApplicationBar.Buttons[0];
        }

        private void LoginCompleted(LoginInfo info)
        {
            Dispatcher.BeginInvoke(() =>
            {
                progLogin.IsLoading = false;

                if (info == null)
                {
                    MessageBox.Show(DropBoxResources.LoginFailure,
                        DropBoxResources.LoginTitle,
                        MessageBoxButton.OK);

                    return;
                }

                this.NavigateTo<List>(
                    "token={0}&secret={1}&path=/",
                    info.Token, info.Secret);
            });
        }

        private void PerformLogin()
        {
            if (!Network.CheckNetwork())
                return;

            progLogin.IsLoading = true;

            new Client().LoginAsync(
                txtEmail.Text,
                txtPassword.Password,
                LoginCompleted);
        }

        private void cmdLogin_Click(object sender, EventArgs e)
        {
            PerformLogin();
        }

        private void txtEmail_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.PlatformKeyCode == 0x0A)
                txtPassword.Focus();
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.PlatformKeyCode == 0x0A)
                PerformLogin();
        }

        private void txt_Changed(object sender, EventArgs e)
        {
            var hasData = txtEmail.Text.Length > 0 &&
                txtPassword.Password.Length > 0;

            cmdLogin.IsEnabled = hasData;
            _cmdAppBarOpen.IsEnabled = hasData;
        }
    }
}