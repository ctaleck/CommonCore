﻿#if __IOS__
using System;
using Foundation;
using UIKit;
using Xamarin.Forms.CommonCore;
using MessageUI;

[assembly: Xamarin.Forms.Dependency(typeof(Communication))]
namespace Xamarin.Forms.CommonCore
{
    public partial class Communication : ICommunication
    {

        public void PlaceCall(string phoneNumber)
        {
            var currentNumber = CoreExtensions.ToNumericString(phoneNumber);

            if (UIApplication.SharedApplication.CanOpenUrl(new NSUrl("telprompt://" + currentNumber)))
            {
                try
                {
                    UIApplication.SharedApplication.OpenUrl(new NSUrl("telprompt://" + currentNumber));
                }
                catch (Exception ex)
                {
                    var m = ex.Message;
                }
            }
            else
            {
                var alert = new UIAlertView("Phone Not Enabled", "This device does not support phone calls", null, "Ok", null);
                alert.Show();
            }
        }

        public void PlaceCallWithCallBack(string phoneNumber, string callBackKey)
        {
            TelephoneManager.CallBackKey = callBackKey;
            var currentNumber = CoreExtensions.ToNumericString(phoneNumber);

            if (UIApplication.SharedApplication.CanOpenUrl(new NSUrl("telprompt://" + currentNumber)))
            {
                TelephoneManager.IsListening = true;
                try
                {
                    UIApplication.SharedApplication.OpenUrl(new NSUrl("telprompt://" + currentNumber));
                }
                catch (Exception ex)
                {
                    var m = ex.Message;
                }
            }
            else
            {
                var alert = new UIAlertView("Phone Not Enabled", "This device does not support phone calls", null, "Ok", null);
                alert.Show();
            }
        }


        public void SendEmail(EmailMessage message)
        {
            if (MFMailComposeViewController.CanSendMail)
            {
                var mailController = new MFMailComposeViewController();
                mailController.SetToRecipients(new string[] { message.EmailAddress });
                mailController.SetSubject(message.Subject);
                mailController.SetMessageBody(message.Message, false);
                mailController.Finished += (object s, MFComposeResultEventArgs args) =>
                {
                    Device.BeginInvokeOnMainThread(() =>
	                {
	                    args.Controller.DismissViewController(true, null);
	                });
                };

                GetUIController().PresentViewController(mailController, true, null);


            }
            else
            {
                var alert = new UIAlertView("Mail not supported", "Can't send mail from this device", null, "OK");
                alert.Show();
            }


        }


        public void SendSMS(string phoneNumber, string message)
        {

            message = message ?? string.Empty;

            if (MFMessageComposeViewController.CanSendText)
            {
                var _smsController = new MFMessageComposeViewController();

                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    string[] recipients = phoneNumber.Split(';');
                    if (recipients.Length > 0)
                        _smsController.Recipients = recipients;
                }

                _smsController.Body = message;

                EventHandler<MFMessageComposeResultEventArgs> handler = null;
                handler = (sender, args) =>
                {
                    _smsController.Finished -= handler;

                    var uiViewController = sender as UIViewController;
                    if (uiViewController == null)
                    {
                        throw new ArgumentException("sender");
                    }

                    uiViewController.DismissViewController(true, () => { });
                };

                _smsController.Finished += handler;

                var vc = GetUIController();
                vc.PresentViewController(_smsController, true, null);

            }
            else
            {
                var alert = new UIAlertView("SMS not supported", "Can't send sms from this device", null, "OK");
                alert.Show();
            }

        }

        private UIViewController GetUIController()
        {
            var win = UIApplication.SharedApplication.KeyWindow;
            var vc = win.RootViewController;
            while (vc.PresentedViewController != null)
                vc = vc.PresentedViewController;
            return vc;
        }
    }

}
#endif
