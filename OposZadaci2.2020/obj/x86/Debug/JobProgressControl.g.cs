﻿#pragma checksum "C:\Users\Jelena\source\repos\OposZadaci2.2020\OposZadaci2.2020\JobProgressControl.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "5FBC70AD05FDE71806E0DD1C76BC5A6A"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OposZadaci2._2020
{
    partial class JobProgressControl : 
        global::Windows.UI.Xaml.Controls.UserControl, 
        global::Windows.UI.Xaml.Markup.IComponentConnector,
        global::Windows.UI.Xaml.Markup.IComponentConnector2
    {
        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 10.0.18362.1")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 2: // JobProgressControl.xaml line 12
                {
                    global::Windows.UI.Xaml.Controls.Grid element2 = (global::Windows.UI.Xaml.Controls.Grid)(target);
                    ((global::Windows.UI.Xaml.Controls.Grid)element2).RightTapped += this.Grid_RightTapped;
                }
                break;
            case 3: // JobProgressControl.xaml line 15
                {
                    global::Windows.UI.Xaml.Controls.MenuFlyoutItem element3 = (global::Windows.UI.Xaml.Controls.MenuFlyoutItem)(target);
                    ((global::Windows.UI.Xaml.Controls.MenuFlyoutItem)element3).Click += this.MenuFlyoutItem_Click;
                }
                break;
            case 4: // JobProgressControl.xaml line 30
                {
                    this.JobTitleTextBlock = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 5: // JobProgressControl.xaml line 31
                {
                    this.JobProgressBar = (global::Windows.UI.Xaml.Controls.ProgressBar)(target);
                }
                break;
            case 6: // JobProgressControl.xaml line 32
                {
                    this.CancelButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.CancelButton).Click += this.CancelButton_Click;
                }
                break;
            case 7: // JobProgressControl.xaml line 35
                {
                    this.PauseButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.PauseButton).Click += this.PauseButton_Click;
                }
                break;
            case 8: // JobProgressControl.xaml line 40
                {
                    this.ResumeButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.ResumeButton).Click += this.ResumeButton_Click;
                }
                break;
            case 9: // JobProgressControl.xaml line 43
                {
                    this.NumberOfThreads = (global::Windows.UI.Xaml.Controls.TextBox)(target);
                    ((global::Windows.UI.Xaml.Controls.TextBox)this.NumberOfThreads).TextChanged += this.NumberOfThreads_TextChanged;
                }
                break;
            default:
                break;
            }
            this._contentLoaded = true;
        }

        /// <summary>
        /// GetBindingConnector(int connectionId, object target)
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 10.0.18362.1")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Windows.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Windows.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}

