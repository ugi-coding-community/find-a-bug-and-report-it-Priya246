﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using NUnit.Framework.Internal;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.PageObjects;
using OpenQA.Selenium.Support.UI;
using Telimena.WebApp.UiStrings;
using Telimena.WebApp.UITests.Base;
using SeleniumExtras.WaitHelpers;
using ExpectedConditions = SeleniumExtras.WaitHelpers.ExpectedConditions;

namespace Telimena.WebApp.UITests.Navigation
{
    [TestFixture]
    public class NavigationTests : PortalTestBase
    {
        [Test]
        public void GoThroughAllPagesAsAdmin()
        {
            try
            {
                this.GoToAdminHomePage();

                WebDriverWait wait = new WebDriverWait(this.Driver, TimeSpan.FromSeconds(15));

                this.Driver.FindElement(By.Id(Strings.Id.AppsAdminDashboardLink)).Click();
                wait.Until(ExpectedConditions.ElementIsVisible(By.Id(@Strings.Id.AppsSummary)));

                this.Driver.FindElement(By.Id(Strings.Id.ToolkitManagementLink)).Click();
                wait.Until(ExpectedConditions.ElementIsVisible(By.Id(@Strings.Id.ToolkitManagementForm)));

                this.Driver.FindElement(By.Id(Strings.Id.PortalAdminDashboardLink)).Click();
                wait.Until(ExpectedConditions.ElementIsVisible(By.Id(@Strings.Id.PortalSummary)));

                this.Driver.FindElement(By.Id(Strings.Id.PortalUsersLink)).Click();
                wait.Until(ExpectedConditions.ElementIsVisible(By.Id(@Strings.Id.PortalUsersTable)));


            }
            catch (Exception ex)
            {
                this.HandlerError(ex);
            }
        }


    }
}