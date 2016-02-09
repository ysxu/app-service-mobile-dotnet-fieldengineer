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

namespace FieldEngineer.Controllers
{
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
        public ActionResult Push()
        {

            // pull all persons from AAD security group

            List<string> members = new List<string>();
            members.Add("Mimi Xu");
            members.Add("Test Person");
            ViewData["members"] = new SelectList(members);

            return View();
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
            return View();

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
