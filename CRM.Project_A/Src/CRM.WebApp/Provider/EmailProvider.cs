﻿using EntityLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.IO;
using System.Threading.Tasks;
using CRM.WebApp.Models;
using System.Data.Entity;
using CRM.WebApp.Infrastructure;

namespace CRM.WebApp.Provider
{
    public class EmailProvider
    {
        private readonly DataBaseCRMEntityes db = new DataBaseCRMEntityes();
        private readonly ModelFactory factory = new ModelFactory();
        private string GetMessageText(int templateId, ContactResponseModel contact)
        {
            try
            {
                var template = db.Templates.Find(templateId);
                var path = HttpContext.Current?.Request.MapPath(template.Path);
                var templateText = File.ReadAllText(path);
                return
                    templateText.Replace("{FullName}", contact.FullName)
                        .Replace("{CompanyName}", contact.CompanyName)
                        .Replace("{Position}", contact.Position)
                        .Replace("{Country}", contact.Country)
                        .Replace("{Email}", contact.Email)
                        .Replace("{DateTimeNow}", DateTime.UtcNow.ToString());
            }
            catch
            {
                throw;
            }
        }

        public void SendEmail(ContactResponseModel contact, int templateId)//List<Contact> list)
        {

            using (var msg = new MailMessage())
            {

                msg.To.Add(contact.Email);
                msg.Subject = "Team BETA";
                msg.IsBodyHtml = true;
                msg.Body = GetMessageText(templateId, contact);

                var client = new SmtpClient();

                try
                {
                    client.Send(msg);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);

                }

            }

        }

        public void SendEmailList(List<ContactResponseModel> list, int TemplateID)
        {
            foreach (var contact in list)
                SendEmail(contact, TemplateID);
        }
        public async Task<bool> SendMailToMailingList(int emailListId, int templateId)
        {
            try
            {
                if (!(await db.Templates.Select(x => x.TemplateId).ToListAsync()).Contains(templateId))
                    return false;
                var emailList = await db.EmailLists.FirstOrDefaultAsync(x => x.EmailListID == emailListId);
                if (emailList == null) return false;

                var contacts = emailList.Contacts.ToList();
                var response = new List<ContactResponseModel>();
                foreach (var item in contacts)
                {
                    response.Add(factory.CreateContactResponseModel(item));
                }
                if (contacts.Count == 0) return false;
                SendEmailList(response, templateId);
                Task.WaitAll();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}