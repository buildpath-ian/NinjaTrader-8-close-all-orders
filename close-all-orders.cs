using System;
using System.Windows;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NinjaTrader.NinjaScript.AddOns
{
    public class CancelPendingOrdersAddOn : AddOnBase
    {
        private CancelPendingOrdersWindow window;
        private NTMenuItem myNewMenuItem;
        private NTMenuItem existingControlCenterNewMenu;
        private static CancelPendingOrdersAddOn instance;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Cancels all pending orders across all accounts";
                Name = "Cancel Pending Orders Add-On";
                instance = this;
            }
            else if (State == State.Terminated)
            {
                if (window != null)
                {
                    window.Dispatcher.InvokeShutdown();
                    window = null;
                }

                RemoveMenuItem();
            }
        }

        protected override void OnWindowCreated(Window window)
        {
            ControlCenter cc = window as ControlCenter;
            if (cc == null)
                return;

            existingControlCenterNewMenu = cc.FindFirst("ControlCenterMenuItemNew") as NTMenuItem;
            if (existingControlCenterNewMenu == null)
                return;

            myNewMenuItem = new NTMenuItem 
            { 
                Header = "Cancel Pending Orders", 
                Style = Application.Current.TryFindResource("MainMenuItem") as Style 
            };

            existingControlCenterNewMenu.Items.Add(myNewMenuItem);
            myNewMenuItem.Click += OnMenuItemClick;
        }

        private void RemoveMenuItem()
        {
            if (myNewMenuItem != null && existingControlCenterNewMenu != null)
            {
                existingControlCenterNewMenu.Items.Remove(myNewMenuItem);
                myNewMenuItem.Click -= OnMenuItemClick;
                myNewMenuItem = null;
            }
        }

        private void OnMenuItemClick(object sender, RoutedEventArgs e)
        {
            instance.Print("OnMenuItemClick method called");
            
            Thread newWindowThread = new Thread(new ThreadStart(() =>
            {
                window = new CancelPendingOrdersWindow();
                window.SetAddOn(this);
                window.Show();
                System.Windows.Threading.Dispatcher.Run();
            }));

            newWindowThread.SetApartmentState(ApartmentState.STA);
            newWindowThread.IsBackground = true;
            newWindowThread.Start();

            instance.Print("New thread started for CancelPendingOrdersWindow");
        }

        private void CancelAllPendingOrders()
        {
            instance.Print("CancelAllPendingOrders method called");
            int cancelledOrdersCount = 0;

            foreach (Account account in Account.All)
            {
                List<Order> pendingOrders = account.Orders.Where(o => o.OrderState == OrderState.Working).ToList();
                if (pendingOrders.Any())
                {
                    account.Cancel(pendingOrders);
                    cancelledOrdersCount += pendingOrders.Count;
                }
            }

            string message = $"Cancelled {cancelledOrdersCount} pending orders across all accounts.";
            instance.Print($"CancelPendingOrdersAddOn: {message}");
            MessageBox.Show(message, "Cancel Pending Orders Complete");
        }

        public class CancelPendingOrdersWindow : Window
        {
            private CancelPendingOrdersAddOn addOn;

            public CancelPendingOrdersWindow()
            {
                Title = "Cancel Pending Orders";
                Width = 300;
                Height = 150;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

                var button = new System.Windows.Controls.Button
                {
                    Content = "Cancel All Pending Orders",
                    Margin = new Thickness(10),
                    Padding = new Thickness(5)
                };
                button.Click += (sender, e) => 
                {
                    instance.Print("Cancel All Pending Orders button clicked");
                    if (addOn != null)
                        addOn.CancelAllPendingOrders();
                    else
                        instance.Print("addOn is null in CancelPendingOrdersWindow");
                };

                Content = button;
            }

            public void SetAddOn(CancelPendingOrdersAddOn addOn)
            {
                this.addOn = addOn;
                instance.Print("SetAddOn called in CancelPendingOrdersWindow");
            }

            protected override void OnClosed(EventArgs e)
            {
                base.OnClosed(e);
                instance.Print("CancelPendingOrdersWindow OnClosed called");
                Dispatcher.InvokeShutdown();
            }
        }
    }
}