﻿using esencialAdmin.Data.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using esencialAdmin.Models.PlanViewModels;
using System.Collections.Generic;
using esencialAdmin.Models.GoodiesViewModels;
using esencialAdmin.Extensions;
using esencialAdmin.Models.SubscriptionViewModels;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace esencialAdmin.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        protected readonly esencialAdminContext _context;
        private readonly IHostingEnvironment _hostingEnvironment;


        public SubscriptionService(esencialAdminContext context, IHostingEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;

        }


        public int createNewSubscription(SubscriptionCreateViewModel newSubscription)
        {
            using (var dbContextTransaction = this._context.Database.BeginTransaction())
            {

                try
                {
                    var plan = this._context.Plans.Where(x => x.Id == newSubscription.PlanID).FirstOrDefault();
                    if (plan == null)
                    {
                        return 0;

                    }

                    var s = new Subscription()
                    {
                        FkCustomerId = newSubscription.CustomerID,
                        FkPlanId = newSubscription.PlanID,
                        PlantNumber = newSubscription.PlantNumber,
                        FkSubscriptionStatusNavigation = _context.SubscriptionStatus.Where(x => x.Label == "Aktiv").FirstOrDefault()
                    };
                    this._context.Subscription.Add(s);
                    this._context.SaveChanges();

                    var currentDeadline = new DateTime(DateTime.UtcNow.Year, plan.Deadline.Month, plan.Deadline.Day);
                    var periodeEnd = new DateTime(currentDeadline.Year, 12, 31);
                    if (newSubscription.StartDate > currentDeadline)
                    {
                        periodeEnd = periodeEnd.AddYears((plan.Duration + 1));
                    }
                    else
                    {
                        periodeEnd = periodeEnd.AddYears(plan.Duration);
                    }
                    var p = new Periodes()
                    {
                        FkSubscriptionId = s.Id,
                        StartDate = newSubscription.StartDate,
                        EndDate = periodeEnd,
                        Price = plan.Price
                    };

                    if (newSubscription.Payed)
                    {
                        p.Payed = true;
                        p.PayedDate = DateTime.UtcNow;
                        p.FkPayedMethodId = newSubscription.PaymentMethodID;
                    }

                    if (newSubscription.GiverCustomerId != 0)
                    {
                        p.FkGiftedById = newSubscription.GiverCustomerId;
                    }

                    int startYear = periodeEnd.Year - plan.Duration;
                    
                    for(int i = 0; i < plan.Duration; i++)
                    {
                        PeriodesGoodies newGoodie = new PeriodesGoodies();
                        
                        newGoodie.FkPlanGoodiesId = plan.Id;
                        newGoodie.SubPeriodeYear = startYear;
                        startYear++;
                        p.PeriodesGoodies.Add(newGoodie);
                    }
                    

                    this._context.Periodes.Add(p);
                    this._context.SaveChanges();
                    dbContextTransaction.Commit();
                    return s.Id;

                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();

                }
            }
            return 0;
        }

        public bool deletePlan(int id)
        {
            try
            {
                this._context.Plans.Remove(this._context.Plans.Where(r => r.Id == id).FirstOrDefault());
                this._context.SaveChanges();
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public List<GoodiesViewModel> getAvailableGoodies()
        {
            List<GoodiesViewModel> goodiesList = new List<GoodiesViewModel>();

            foreach (PlanGoodies goody in _context.PlanGoodies)
            {
                goodiesList.Add(new GoodiesViewModel
                {
                    Id = goody.Id,
                    Name = goody.Name
                });
            }

            return goodiesList;
        }

        public List<PaymentMethodsViewModel> getAvailablePaymentMethods()
        {
            List<PaymentMethodsViewModel> paymentList = new List<PaymentMethodsViewModel>();

            foreach (PaymentMethods method in _context.PaymentMethods)
            {
                paymentList.Add(new PaymentMethodsViewModel
                {
                    Id = method.Id,
                    Name = method.Name
                });
            }

            return paymentList;
        }

        public JsonResult getSelect2Customers(string searchTerm, int pageSize, int pageNum)
        {
            try
            {

                // Getting all Customer data  
                var customerData = (from tmpcustomer in _context.Customers
                                    select new { Id = tmpcustomer.Id, DisplayString = ((tmpcustomer.Company ?? "") + " " + (tmpcustomer.FirstName ?? "") + " " + (tmpcustomer.LastName ?? "") + " " + (tmpcustomer.Street ?? "") + " " + (tmpcustomer.Zip ?? "") + " " + (tmpcustomer.City ?? "")).Replace("  ", " ").Trim() });

                //Sorting  
                customerData = customerData.OrderBy(x => x.DisplayString);

                //Search  
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    customerData = customerData.Where(m => m.DisplayString.Contains(searchTerm));
                }

                //total number of rows count   
                var recordsTotal = customerData.Count();
                var skip = pageNum * pageSize;
                //Paging   
                var data = customerData.Skip(skip).Take(pageSize).ToList();

                var resultList = new List<Select2Result>();
                foreach (var item in data)
                {
                    resultList.Add(new Select2Result()
                    {
                        text = item.DisplayString.Replace("  ", " "),
                        id = item.Id.ToString()
                    });
                }

                //Returning Json Data  
                return new JsonResult(new { items = resultList, total = recordsTotal });

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public JsonResult getSelect2Plans(string searchTerm, int pageSize, int pageNum)
        {
            try
            {

                // Getting all Plan data  
                var planData = (from tmpplan in _context.Plans
                                select new { Id = tmpplan.Id, DisplayString = ((tmpplan.Name ?? "") + " - Laufzeit: " + (tmpplan.Duration.ToString() ?? "") + " Jahre - Preis " + (tmpplan.Price.ToString("N2") ?? "")).Replace("  ", " ").Trim() });

                //Sorting  
                planData = planData.OrderBy(x => x.DisplayString);

                //Search  
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    planData = planData.Where(m => m.DisplayString.Contains(searchTerm));
                }

                //total number of rows count   
                var recordsTotal = planData.Count();
                var skip = pageNum * pageSize;
                //Paging   
                var data = planData.Skip(skip).Take(pageSize).ToList();

                var resultList = new List<Select2Result>();
                foreach (var item in data)
                {
                    resultList.Add(new Select2Result()
                    {
                        text = item.DisplayString.Replace("  ", " "),
                        id = item.Id.ToString()
                    });
                }

                //Returning Json Data  
                return new JsonResult(new { items = resultList, total = recordsTotal });

            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public JsonResult loadDefaultSubscriptionDataTable(HttpRequest Request)
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                // Skiping number of Rows count  
                var start = Request.Form["start"].FirstOrDefault();
                // Paging Length 10,20  
                var length = Request.Form["length"].FirstOrDefault();
                // Sort Column Name                  
                var columnIndex = Request.Form["order[0][column]"].ToString();

                // var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                string sortColumn = Request.Form[$"columns[{columnIndex}][data]"].ToString();

                var sortDirection = Request.Form["order[0][dir]"].ToString();
                // Sort Column Direction ( asc ,desc)  
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                // Search Value from (Search box)  
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                //Paging Size (10,20,50,100)  
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                // Getting all Customer data  
                var planData = (from tempplan in _context.Subscription
                                select new
                                {
                                    Id = tempplan.Id,
                                    PlantNr = tempplan.PlantNumber,
                                    Customer = tempplan.FkCustomer.FirstName + " " + tempplan.FkCustomer.LastName,
                                    Plan = tempplan.FkPlan.Name,
                                    Periode = tempplan.Periodes.Where(x => x.EndDate > DateTime.UtcNow).First().StartDate.ToString("dd.MM.yyyy") + " -<br>" + tempplan.Periodes.Where(x => x.EndDate > DateTime.UtcNow).First().EndDate.ToString("dd.MM.yyyy"),
                                    Payed = tempplan.Periodes.Where(x => x.EndDate > DateTime.UtcNow).First().Payed ? "Ja" : "Nein"
                                });
                //select new {Id = tmpcustomer.Id, DisplayString = ((tmpcustomer.Company ?? "") + " " + (tmpcustomer.FirstName ?? "") + " " + (tmpcustomer.LastName ?? "") + " " + (tmpcustomer.Street ?? "") + " " + (tmpcustomer.Zip ?? "") + " " + (tmpcustomer.City ?? "")).Replace("  "," ").Trim() });

                //Sorting  
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    sortColumn = sortColumn.Substring(0, 1).ToUpper() + sortColumn.Remove(0, 1);
                    planData = planData.OrderBy(sortColumn + ' ' + sortColumnDirection);
                }
                //Search  
                if (!string.IsNullOrEmpty(searchValue))
                {
                    planData = planData.Where(m => m.Customer.StartsWith(searchValue));
                }

                //total number of rows count   
                recordsTotal = planData.Count();
                //Paging   
                var data = planData.Skip(skip).Take(pageSize).ToList();

                //Returning Json Data  
                return new JsonResult(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public SubscriptionEditViewModel loadSubscriptionInputModel(int id)
        {
            var subscriptionToLoad = this._context.Subscription
                  .Include(c => c.FkCustomer)
                  .Include(c => c.FkPlan).ThenInclude(x => x.FkGoody)
                  .Where(c => c.Id == id)
                  .FirstOrDefault();

            if (subscriptionToLoad == null)
            {
                return null;
            }


            var subscriptionToEdit = new SubscriptionEditViewModel();
            subscriptionToEdit.ID = subscriptionToLoad.Id;
            subscriptionToEdit.PlantNumber = subscriptionToLoad.PlantNumber ?? 0;
            subscriptionToEdit.Customer = SubscriptionCustomerViewModel.CreateFromCustomer(subscriptionToLoad.FkCustomer);
            subscriptionToEdit.Plan = SubscriptionPlanViewModel.CreateFromPlan(subscriptionToLoad.FkPlan);

            var periodesToLoad = this._context.Periodes
                .Include(c => c.FkGiftedBy)
                .Include(c => c.PeriodesGoodies)
                .Where(c => c.FkSubscriptionId == subscriptionToLoad.Id);

            subscriptionToEdit.Periodes = new List<SubscriptionPeriodeViewModel>();
            DateTime currentDeadLine = new DateTime(DateTime.UtcNow.Year, subscriptionToLoad.FkPlan.Deadline.Month, subscriptionToLoad.FkPlan.Deadline.Day);
            bool isCurrentDayAfterDeadLine = false;
            if (currentDeadLine > DateTime.UtcNow.Date)
            {
                isCurrentDayAfterDeadLine = true;
            }
            foreach (Periodes p in periodesToLoad)
            {
                SubscriptionPeriodeViewModel pModel = SubscriptionPeriodeViewModel.CreateFromPeriode(p);
                if (p.StartDate < DateTime.UtcNow && p.EndDate > DateTime.UtcNow)
                {
                    if (!(p.EndDate.Year == DateTime.UtcNow.Year && isCurrentDayAfterDeadLine))
                    {
                        pModel.CurrentPeriode = true;
                    }
                }
                foreach (PeriodesGoodies pg in p.PeriodesGoodies)
                {
                    pModel.Goodies.Add(SubscriptionPeriodesGoodiesViewModel.CreateFromGoodie(pg));
                }

                pModel.PaymentMethods = getAvailablePaymentMethods();
                pModel.GoodiesLabel = subscriptionToLoad.FkPlan.FkGoody.Name;
                subscriptionToEdit.Periodes.Add(pModel);

            }
            subscriptionToEdit.Photos = new Dictionary<int, String>();
            foreach (var photo in _context.SubscriptionPhotos.Where(x => x.FkSubscriptionId == id).Select(x =>  new {x.FkFileId, x.FkFile.OriginalName }))
            {
                subscriptionToEdit.Photos.Add(photo.FkFileId,"/file/img/" + id + '/' + photo.OriginalName.Insert(photo.OriginalName.LastIndexOf("."),"_thumb"));
            }


            if (subscriptionToLoad.DateCreated != null)
            {
                subscriptionToEdit.DateCreated = subscriptionToLoad?.DateCreated.Value.ToLocalTime();
            }
            if (subscriptionToLoad.DateModified != null)
            {
                subscriptionToEdit.DateModified = subscriptionToLoad?.DateModified.Value.ToLocalTime();

            }

           
            subscriptionToEdit.UserCreated = subscriptionToLoad?.UserCreated;
            subscriptionToEdit.UserModified = subscriptionToLoad?.UserModified;


            return subscriptionToEdit;
        }

        public bool updatePlan(PlanInputViewModel planToUpdate)
        {
            try
            {
                var planToEdit = this._context.Plans
                  .Where(c => c.Id == planToUpdate.ID)
                  .FirstOrDefault();
                if (planToEdit == null)
                {
                    return false;
                }
                planToEdit.Name = planToUpdate.Name;
                planToEdit.Price = planToUpdate.Price;
                planToEdit.Duration = planToUpdate.Duration;
                planToEdit.Deadline = planToUpdate.Deadline;
                planToEdit.FkGoodyId = planToUpdate.GoodyID;

                this._context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public async System.Threading.Tasks.Task<bool> addSubscriptionPhoto(IFormFile formFile, int subscriptionID)
        {
            try
            {
                var subscription = this._context.Subscription
                             .Where(c => c.Id == subscriptionID)
                             .FirstOrDefault();
                if (subscription == null)
                {
                    return false;
                }

                string webRootPath = _hostingEnvironment.WebRootPath;
                string contentRootPath = _hostingEnvironment.ContentRootPath;

                string customerPath = _hostingEnvironment.ContentRootPath + "\\Data\\Userdata\\" + subscription.FkCustomerId;
                if (!Directory.Exists(customerPath))
                {
                    Directory.CreateDirectory(customerPath);
                }
                string subscriptionPath = customerPath + "\\" + subscription.Id;
                if (!Directory.Exists(subscriptionPath))
                {
                    Directory.CreateDirectory(subscriptionPath);
                }

                String newFileName = Guid.NewGuid().ToString();

                while (File.Exists(subscriptionPath + "\\" + newFileName))
                {
                    newFileName = Guid.NewGuid().ToString();
                }

                using (var fileStream = new FileStream(subscriptionPath + "\\" + newFileName, FileMode.Create))
                {
                    await formFile.CopyToAsync(fileStream);
                }

                String originalFileName = formFile.FileName;
                int count = 1;
                while (_context.SubscriptionPhotos.Where(x => x.FkSubscriptionId == subscriptionID && x.FkFile.OriginalName == originalFileName).Any())
                {
                    originalFileName = formFile.FileName;
                    originalFileName = originalFileName.Insert(originalFileName.LastIndexOf("."), "_" + count);
                    count++;
                }

                var newFile = new Files();
                newFile.OriginalName = originalFileName;
                newFile.Path = "\\Data\\Userdata\\" + subscription.FkCustomerId + "\\" + subscription.Id + "\\" ;
                newFile.FileName = newFileName;
                newFile.ContentType = formFile.ContentType;
                this._context.Files.Add(newFile);

                var newSubscriptionPhotos = new SubscriptionPhotos();
                newSubscriptionPhotos.FkFile = newFile;
                newSubscriptionPhotos.FkSubscription = subscription;

                this._context.SubscriptionPhotos.Add(newSubscriptionPhotos);

                this._context.SaveChanges();

                return true;
            }
            catch(Exception ex)
            {

            }

            return false;
            
        }

        public bool updatePaymentStatus(int periodeID, bool isPayed)
        {
            try
            {
                var periodeToEdit = this._context.Periodes
                  .Where(c => c.Id == periodeID)
                  .FirstOrDefault();
                if (periodeToEdit == null)
                {
                    return false;
                }
                periodeToEdit.Payed = isPayed;

                if (isPayed)
                {
                    periodeToEdit.PayedDate = DateTime.UtcNow;
                }
                else
                {
                    periodeToEdit.PayedDate = null;                   
                }

                this._context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public bool updateReceivedGoody(int goodyID, bool hasReceived)
        {
            try
            {
                var goodyToEdit = this._context.PeriodesGoodies
                  .Where(c => c.Id == goodyID)
                  .FirstOrDefault();
                if (goodyToEdit == null)
                {
                    return false;
                }
                goodyToEdit.Received = hasReceived;

                if (hasReceived)
                {
                    goodyToEdit.ReceivedAt = DateTime.UtcNow;
                }
                else
                {
                    goodyToEdit.ReceivedAt = null;
                }

                this._context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public bool updatePaymentMethod(int periodeID, int paymentID)
        {
            try
            {
                var periodeToEdit = this._context.Periodes
                  .Where(c => c.Id == periodeID)
                  .FirstOrDefault();
                if (periodeToEdit == null)
                {
                    return false;
                }

                if(this._context.PaymentMethods.Any(x => x.Id == paymentID))
                {
                    periodeToEdit.FkPayedMethodId = paymentID;
                }
                else
                {
                    return false;
                }

                this._context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}