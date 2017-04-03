//
//  Copyright 2012-2016, Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
using System;
using System.Threading.Tasks;
using System.Text;

using Xamarin.Utilities.iOS;
using Xamarin.Controls;

#if !__UNIFIED__
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#else
using Foundation;
using UIKit;
using WebKit;
#endif

namespace Xamarin.Auth
{
    internal partial class WebAuthenticatorController
    {
        //==============================================================================================================
        #region     UIWebView
        /// <summary>
        /// UIWebView UIWebViewDelegate
        /// </summary>
        internal partial class UIWebViewDelegate : UIKit.UIWebViewDelegate
        {
            protected WebAuthenticatorController controller;
            Uri lastUrl;


            public UIWebViewDelegate(WebAuthenticatorController controller)
            {
                this.controller = controller;
            }

            /// <summary>
            /// Whether the UIWebView should begin loading data.
            /// </summary>
            /// <returns><c>true</c>, if start load was shoulded, <c>false</c> otherwise.</returns>
            /// <param name="webView">Web view.</param>
            /// <param name="request">Request.</param>
            /// <param name="navigationType">Navigation type.</param>
            public override bool ShouldStartLoad(UIWebView webView, NSUrlRequest request, UIWebViewNavigationType navigationType)
            {
                NSUrl nsUrl = request.Url;

                string msg = null;
                #if DEBUG
                msg = String.Format("WebAuthenticatorController.ShouldStartLoad {0}", nsUrl.AbsoluteString);
                System.Diagnostics.Debug.WriteLine(msg);
                #endif

                WebRedirectAuthenticator wra = null;
                wra = (WebRedirectAuthenticator)this.controller.authenticator;

                if (nsUrl != null && !controller.authenticator.HasCompleted)
                {
                    Uri url;
                    if (Uri.TryCreate(nsUrl.AbsoluteString, UriKind.Absolute, out url))
                    {
                        string host = url.Host.ToLower();
                        string scheme = url.Scheme;

                        #if DEBUG
                        msg = String.Format("WebAuthenticatorController.ShouldStartLoad {0}", url.AbsoluteUri);
                        System.Diagnostics.Debug.WriteLine(msg);
                        msg = string.Format("                          Host   = {0}", host);
                        System.Diagnostics.Debug.WriteLine(msg);
                        msg = string.Format("                          Scheme = {0}", scheme);
                        System.Diagnostics.Debug.WriteLine(msg);
                        #endif

                        if (host == "localhost" || host == "127.0.0.1" || host == "::1")
                        {
                            wra.IsLoadableRedirectUri = false;
                            this.controller.DismissViewControllerAsync(true);
                        }
                        else
                        {
                            wra.IsLoadableRedirectUri = true;
                        }

                        controller.authenticator.OnPageLoading(url);
                    }
                }

                return wra.IsLoadableRedirectUri;
            }

            public override void LoadStarted(UIWebView webView)
            {
                controller.activity.StartAnimating();

                webView.UserInteractionEnabled = false;
            }

            public override void LoadFailed(UIWebView webView, NSError error)
            {
                if (error.Domain == "WebKitErrorDomain")
                {
                    if (error.Code == 102)
                    {
                        // 
                        // WebViewDelegate.ShouldStartLoad returned false
                        // localhost, 127.0.0.1, ::1
                        // TODO: custom uris
                        // No need to show error - return immediately
                        return;
                    }
                }
                else if (error.Domain == "NSURLErrorDomain")
                {
                    // {The operation couldn’t be completed. (NSURLErrorDomain error -999.)}
                    if (error.Code == -999)
                    {
                        // delegate is getting a "cancelled" (-999) failure, 
                        //      that might be originated in javascript or 
                        //      fast clicks!!
                        //      perhaps even in a UIWebView bug.
                        return;
                    }
                }
                else

                    controller.activity.StopAnimating();

                webView.UserInteractionEnabled = true;

                controller.authenticator.OnError(error.LocalizedDescription);

                return;
            }

            public override void LoadingFinished(UIWebView webView)
            {
                controller.activity.StopAnimating();

                webView.UserInteractionEnabled = true;

                var url = new Uri(webView.Request.Url.AbsoluteString);
                if (url != lastUrl && !controller.authenticator.HasCompleted)
                {
                    lastUrl = url;
                    controller.authenticator.OnPageLoaded(url);
                }

                return;
            }
        }
        #endregion  UIWebView
        //==============================================================================================================
    }
}

