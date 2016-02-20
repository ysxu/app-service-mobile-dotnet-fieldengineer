using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FieldEngineerLiteService.DataObjects;
using FieldEngineerLiteService.Models;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server;
using Microsoft.Azure.Mobile.Server.Config;
using Microsoft.Azure.NotificationHubs;
using System.Configuration;
using System.Net.Http;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;

namespace FieldEngineer.Controllers
{
    [System.Web.Mvc.Authorize]
    public class AdminController : Controller
    {
        private JobDbContext db = new JobDbContext();

        // GET: Admin
        public async Task<ActionResult> Index()
        {
            return View(await db.JobsDbSet.ToListAsync());
        }

        // GET: Admin/Details/5
        public async Task<ActionResult> Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Job job = await db.JobsDbSet.FindAsync(id);
            if (job == null)
            {
                return HttpNotFound();
            }
            return View(job);
        }

        // GET: Admin/Create
        public ActionResult Create()
        {
            return View();
        }

        // GET: Admin/Push
        public async Task<ActionResult> Push()
        {
            List<string> members = new List<string>();
            members.Add(" ");

            try
            {
                // The Azure AD Graph API for my directory is available at this URL.
                const string serviceRootURL = "https://graph.windows.net/xumimixugmail.onmicrosoft.com";

                // Instantiate an instance of ActiveDirectoryClient.
                Uri serviceRoot = new Uri(serviceRootURL);
                ActiveDirectoryClient adClient = new ActiveDirectoryClient(
                    serviceRoot,
                    async () => await GetAppTokenAsync());

                IPagedCollection<IUser> pagedCollection = await adClient.Users.ExecuteAsync();
                if (pagedCollection != null)
                {
                    do
                    {
                        List<IUser> userList = pagedCollection.CurrentPage.ToList();
                        foreach (IUser user in userList)
                        {
                            members.Add(user.DisplayName);
                        }
                        pagedCollection = await pagedCollection.GetNextPageAsync();
                    } while (pagedCollection != null);
                }
            }
            catch (Exception e)
            {
                members.Add("Error");
            }
            ViewData["members"] = new SelectList(members);
            return View();
        }

        private static async Task<string> GetAppTokenAsync()
        {
            // This is the URL the application will authenticate at.
            const string authString = "https://login.windows.net/xumimixugmail.onmicrosoft.com";

            // These are the credentials the application will present during authentication
            // and were retrieved from the Azure Management Portal.
            // *** Don't even try to use these - they have been deleted.
            const string clientID = "4d3ed9f2-1bf2-48d0-9d29-a9e6b58bbd10";
            const string clientSecret = "Sfayet4ySmtAaNnFXpUWMu6dfZXQ1p1IEz0+kCAPxFE=";

            // The Azure AD Graph API is the "resource" we're going to request access to.
            const string resAzureGraphAPI = "https://graph.windows.net";

            // Instantiate an AuthenticationContext for my directory (see authString above).
            AuthenticationContext authenticationContext = new AuthenticationContext(authString, false);

            // Create a ClientCredential that will be used for authentication.
            // This is where the Client ID and Key/Secret from the Azure Management Portal is used.
            ClientCredential clientCred = new ClientCredential(clientID, clientSecret);

            // Acquire an access token from Azure AD to access the Azure AD Graph (the resource)
            // using the Client ID and Key/Secret as credentials.
            AuthenticationResult authenticationResult = await authenticationContext.AcquireTokenAsync(resAzureGraphAPI, clientCred);

            // Return the access token.
            return authenticationResult.AccessToken;
        }


        // Handle form in push
        public async Task<ActionResult> HandlePush(string members, string location, string message)
        {
            string tag = "";

            if (!(string.IsNullOrEmpty(members)))
            {
                tag = members.ToLower().Replace(" ", string.Empty);
            }
            else if (!string.IsNullOrEmpty(location))
            {
                tag = location.ToLower().Replace(" ", string.Empty);
            }


            NotificationHubClient hub = NotificationHubClient.CreateClientFromConnectionString("Endpoint=sb://anhdemomimins.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=Y463FsBXLAkaTvEhJSMQUDA67zyBXo0TdC5ERhesl0Q=", "fieldengineerdemomimi");
            var alert = "{\"aps\":{\"alert\":\"" + message + "\",\"sound\":\"default\"}}";

            if (string.IsNullOrEmpty(tag))
            {
                //broadcast to all
                await hub.SendAppleNativeNotificationAsync(alert);
            }
            else
            {
                //send to tag
                await hub.SendAppleNativeNotificationAsync(alert, new[] {tag});
            }

            return RedirectToAction("Index");
        }

        // POST: Admin/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,AgentId,JobNumber,Title,StartTime,EndTime,Status,CustomerName,CustomerAddress,CustomerPhoneNumber,WorkPerformed,Version,CreatedAt,UpdatedAt,Deleted")] Job job)
        {
            if (ModelState.IsValid)
            {
                db.JobsDbSet.Add(job);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(job);
        }

        // GET: Admin/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Job job = await db.JobsDbSet.FindAsync(id);
            if (job == null)
            {
                return HttpNotFound();
            }
            return View(job);
        }

        // POST: Admin/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,AgentId,JobNumber,Title,StartTime,EndTime,Status,CustomerName,CustomerAddress,CustomerPhoneNumber,WorkPerformed,CreatedAt,UpdatedAt,Version,Deleted")] Job job)
        {
            if (ModelState.IsValid)
            {
                db.Entry(job).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(job);
        }

        // GET: Admin/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Job job = await db.JobsDbSet.FindAsync(id);
            if (job == null)
            {
                return HttpNotFound();
            }
            return View(job);
        }

        // POST: Admin/Delete/5
        [System.Web.Mvc.HttpPost, System.Web.Mvc.ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            Job job = await db.JobsDbSet.FindAsync(id);
            db.JobsDbSet.Remove(job);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
