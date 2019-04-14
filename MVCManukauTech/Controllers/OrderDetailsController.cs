using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVCManukauTech.Models.DB;
using MVCManukauTech.ViewModels;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace MVCManukauTech.Controllers
{
    public class OrderDetailsController : Controller
    {
        private readonly F191_tron01_XSpyContext _context;

        public OrderDetailsController(F191_tron01_XSpyContext context)
        {
            _context = context;
        }

        //140825 started JPC John Calder - adapted from XSpy version 1 webforms 2004-2008

        // GET: OrderDetails/ShoppingCart?ProductId=1MOR4ME
        // or to simply view the cart
        // GET: OrderDetails/ShoppingCart
        public IActionResult ShoppingCart()
        {
            string SQLGetOrder = "";
            string SQLStartOrder = "";
            string SQLCart = "";
            string SQLBuy = "";
            string SQLUnitCostLookup = "";
            int rowsChanged = 0;

            if (User.IsInRole("FullMember"))
            {
                //Code for action for a Full Member
            }
            else if (User.IsInRole("Associate"))
            {
                //Code for action for an Associate Member
            }


            string ProductId = Request.Query["ProductId"];
            // Security checking
            if (ProductId != null && (ProductId.Length > 20 || ProductId.IndexOf("'") > -1 || ProductId.IndexOf("#") > -1))
            {
                //
                return BadRequest();
            }

            //Have we created an order for this user yet?
            //If not then create a placeholder for a mostly empty order
            //Note that any Order in progress has an OrderStatusId of 0 or 1
            //We are not interested in Orders with higher status because they have already gone through checkout
            //2015-08-07 JPC Security improvement implementation of @p0
            //2019-03-19 JPC Change of table name from Orders to Order causes keyword clash 
            //  and therefore need to wrap Order in square brackets!
            SQLGetOrder = "SELECT * FROM [Order] WHERE SessionId = @p0 AND OrderStatusId <= 1;";

            //140825 JPC.  We may need 2 attempts at reading the order out of the database.
            //  Managing this as a for..loop with 2 loops.  If successful first time then break out.
            //  (Other opinion Rosemarie T - "this is a bit dodgy!")
            //150807 JPC Security improvement implementation of @p0
            var orders = _context.Order.FromSql(SQLGetOrder, HttpContext.Session.Id).ToList();
            for (int iForLoop = 0; iForLoop <= 1; iForLoop++)
            {
                //Do we have an order?
                if (orders.Count == 1)
                {
                    //we have an order, we can continue to the next step
                    break;
                }
                else if (iForLoop == 1)
                {
                    //failed on the second attempt
                    throw new Exception("ERROR with database table 'Order'.  This session fails to write a new order.");
                }
                else if (orders.Count > 1)
                {
                    //This would be a major error. In theory impossible but we need to be ready for any outcome
                    throw new Exception("ERROR with database table 'Order'.  This session is running more than one shopping cart.");
                }
                else
                {
                    //else we get an order started
                    //150807 JPC Security improvement implementation of @p0
                    SQLStartOrder = "INSERT INTO [Order](SessionId, OrderStatusId) VALUES(@p0, 0);";
                    rowsChanged = _context.Database.ExecuteSqlCommand(SQLStartOrder, HttpContext.Session.Id);
                    // a good result would be one row changed
                    if (rowsChanged != 1)
                    {
                        //Error handling code to go in here.  Poss return a view with error messages.
                        //Code from our old webforms version is -- 
                        throw new Exception("ERROR with database table 'Order'.");
                    }
                    //ready to try reading that order again
                    //150807 JPC Security improvement implementation of @p0, parameter Session.SessionID
                    orders = _context.Order.FromSql(SQLGetOrder, HttpContext.Session.Id).ToList();
                    //go round and test orders again
                }
            }

            //What is the OrderId
            int orderId = orders[0].OrderId;

            //040514 JPC EDUC add ORDER BY clause
            //080704 JPC Note that with use of SQL Server, "LineNo" is a reserved word!
            //  Therefore change to "LineNumber"
            //150807 JPC Security improvement implementation of @p0
            //20180313 JPC temp drop parameter because of problems
            SQLCart = "SELECT OrderDetail.OrderId AS OrderId, OrderDetail.LineNumber As LineNumber, OrderDetail.ProductId As ProductId, " +
                "Product.Name As ProductName, Product.ImageFileName As ImageFileName, " +
                "OrderDetail.Quantity As Quantity, OrderDetail.UnitCost As UnitCost " +
                "FROM OrderDetail INNER JOIN Product ON Product.ProductId = OrderDetail.ProductId " +
                "WHERE OrderDetail.OrderId = @p0 ORDER BY OrderDetail.LineNumber;";
            // Note that this is an "view" query across 2 tables 
            // so we need to create a new VIEW MODEL class "OrderDetailsQueryForCart" to match up
            // See this under folder "ViewModels"
            //150807 JPC Security improvement implementation of @p0, parameter orderId
            var cart = _context.OrderDetailsQueryForCart.FromSql(SQLCart, orderId).ToList();

            //140903 JPC
            //What to do about a product for the case where the user clicked add to cart ..
            //IF the product is already in the cart THEN raise the quantity by one ELSE add it in

            int lineNumber = 0;
            int? quantity = 0;
            //140903 JPC cover case of user is only looking at the cart without adding any changes
            if (ProductId == null)
            {
                //use lineNumber = -1 as a flag to skip the data writing in the following "if" block
                lineNumber = -1;
            }
            else
            {
                foreach (var item in cart)
                {
                    if (item.ProductId == ProductId)
                    {
                        lineNumber = item.LineNumber;
                        quantity = item.Quantity;
                        break;
                    }
                }
            } //end if

            rowsChanged = 0;
            if (lineNumber > 0)
            {
                quantity += 1;
                //150807 JPC Security improvement implementation of @p0, @p1, @p2 - (was {0}, {1}, {2})
                SQLBuy = "UPDATE OrderDetail SET Quantity = @p0 WHERE OrderId = @p1 AND LineNumber = @p2 ";
                rowsChanged = _context.Database.ExecuteSqlCommand(SQLBuy, quantity, orderId, lineNumber);
            }
            else if (lineNumber == 0)
            {
                //writing a new line.  we need to know the unitcost
                //in real life work this could grow into a major method in a separate class involving special member discounts etc
                //here there is a choice between sending it in the querystring, or doing a new database lookup 
                //the querystring is easier but I am concerned about users being able to interfere with querystrings so I will go with the database

                //Safe bet is to stick to the SELECT * approach to match the existing generated classes.  
                //Can also call this the "go with the flow" method
                //150807 JPC Security improvement implementation of @p0
                SQLUnitCostLookup = "SELECT * FROM Product WHERE ProductId = @p0";
                var products = _context.Product.FromSql(SQLUnitCostLookup, ProductId).ToList();
                decimal unitCost = Convert.ToDecimal(products[0].UnitCost);

                lineNumber = cart.Count + 1;
                //150807 JPC Security improvement implementation of @p0 etc
                SQLBuy = "INSERT INTO OrderDetail VALUES(@p0, @p1, @p2, @p3, @p4)";
                rowsChanged = _context.Database.ExecuteSqlCommand(SQLBuy, orderId, lineNumber, ProductId, 1, unitCost);
            }

            //If User has selected a product to add then Query is UPDATE or INSERT but they both run like this
            if (SQLBuy != "")
            {
                if (rowsChanged != 1)
                {
                    //Error handling code to go in here.  Poss return a view with error messages.
                    //Code from our old webforms version is -- 
                    throw new Exception("ERROR with database table 'Order'.");
                }

                //If we have changed the cart in the database, then we need to reload it here
                //140903 JPC note the syntax for working with a "View Model"
                //150807 JPC Security improvement implementation of @p0, parameter orderId
                cart = _context.OrderDetailsQueryForCart.FromSql(SQLCart, orderId).ToList();
            }

            //Give that Session object some work to do to wake it up and get it functional
            HttpContext.Session.SetInt32("OrderId", orderId);
            //20180312 JPC use ViewBag to get the orderId to the cart for display
            ViewBag.OrderId = orderId;
            return View(cart);

        }

        //AJAX!
        // This sample GET string is not useful as a copy/paste test because it only runs as a step in a longer process
        // that would be difficult to test manually.  Quoted as documentation example only
        // GET: OrderDetails/ShoppingCartUpdate?Quantity=4&LineNumber=7

        [HttpPost]
        public string ShoppingCartUpdate(string Quantity, string LineNumber)
        {
            string SQLUpdateOrderDetails = "";
            int rowsChanged = 0;

            //140903 JPC check that Quantity and LineNumber are numeric. Non-numeric or decimal could indicate hacker mischief-making
            //20180312 JPC IsNumeric method is coded at the bottom of this class
            if (!IsNumeric(Quantity) || !IsNumeric(LineNumber)
                || Convert.ToInt32(Quantity) != Convert.ToDouble(Quantity)
                || Convert.ToInt32(LineNumber) != Convert.ToDouble(LineNumber))
            {
                //TODO Code to log this event and send alert email to admin
                return "ERROR";
            }
            int orderId = Convert.ToInt32(HttpContext.Session.GetInt32("OrderId"));
            //150807 JPC Security improvement implementation of @p0 etc
            SQLUpdateOrderDetails = "UPDATE OrderDetail SET Quantity = @p0 WHERE OrderId = @p1 AND LineNumber = @p2";
            rowsChanged = _context.Database.ExecuteSqlCommand(SQLUpdateOrderDetails, Quantity, orderId, LineNumber);
            if (rowsChanged == 1)
            {
                //expected good result
                return "SUCCESS";
            }
            else if (rowsChanged == 0)
            {
                //nothing happened, a bit sad but there is no change so simple feedback is needed
                return "ERROR";
            }
            else
            {
                //more than 1 rows changed is in theory impossible.
                //the only possibility I can think of is some kind of hacking injection attack where % signs
                //get into the mix and give a wider scope to what the WHERE statement finds.
                //if it does happen then we have a major problem on our hands and we need 
                //to cancel this shopping cart immediately 
                //needs SQL DELETE for the cart
                return "ERROR HIGH SEVERITY"; //placeholder only, 
            }

        }

        //First time setup of the form web page
        public IActionResult Checkout()
        {
            CheckoutViewModel checkout = new CheckoutViewModel();
            //Calculate the grand total for display and processing
            //We need to remember the orderId - see above where we "persisted" (remembered) it in "Session"
            int orderId = Convert.ToInt32(HttpContext.Session.GetInt32("OrderId"));

            //Use the orderId to get the GrandTotal out of the database
            //150807 JPC Security improvement implementation of @p0, parameter orderId
            string SQLGetGrandTotal = "SELECT SUM(Quantity * UnitCost) AS GrandTotal FROM OrderDetail WHERE OrderId = @p0";

            //150609 JPC Interesting code here.  Took me a while to do this one mostly by a range of logical guesses and interpretation of error messages.
            //  We need to say that the SQLQuery will give us a type decimal result, 
            //  and we also need to say that there is only a single result
            //  rather than the usual collection that comes from an SQL query.
            //150807 JPC Security improvement implementation of @p0, parameter orderId
            var grandTotals = _context.GrandTotalViewModel.FromSql(SQLGetGrandTotal, orderId).ToList();
            decimal grandTotal = grandTotals[0].GrandTotal;

            //We include grandTotal as an element of class Checkout.  
            checkout.GrandTotal = grandTotal;
            //Also persist because we will need it later for the "Bank of Fiction", or other Credit Card Authority
            HttpContext.Session.SetString("GrandTotal", grandTotal.ToString());

            return View(checkout);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(CheckoutViewModel checkout)
        {
            //Remember that GrandTotal that we persisted earlier
            decimal grandTotal = Convert.ToDecimal(HttpContext.Session.GetString("GrandTotal"));
            //Pessimisti default
            bool isSuccess = false;
            //Pessimistic default - if credit card validation succeeds then change this to 3
            int statusId = 4;
            //Empty defaults
            string SQL = "";
            int rowsChanged = 0;

            //No expiry date in this form design, or in Class Checkout.  Do a hard-coded test.  
            //Students!  Make this better!
            string expiryDatePlaceHolder = "2019-10-01";

            //20180316 JPC change remote messaging from POST to GET for easier testing while working out migration to Core

            //POST data to the "Bank of Fiction"
            //Ref: http://stackoverflow.com/questions/5401501/how-to-post-data-to-specific-url-using-webclient-in-c-sharp
            //Ref: http://en.wikipedia.org/wiki/Percent-encoding


            System.Net.WebClient w = new System.Net.WebClient();
            //POST has some optional configurations - recommended
            w.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            w.Encoding = System.Text.Encoding.UTF8;

            //20180316 JPC change remote messaging from POST to GET for easier testing while working out migration to Core
            //Keeping the POST-style separation of URL and data to make it easier to switch back later
            //POST separates the web address (URI) and the data
            string webAddress = "https://manukautech.info/BankFiction3/Transactions/Reservation";
            //string webAddress = "https://localhost:44398/Transactions/Reservation";
            //ALERT Over-Simplification to get learning started! - note the use of hard-coded bank username and password - in real life we need to do much much better than this!!!
            string data = "MerchantId=Kim@a.a&MerchantPassword=nice.coffee&CardNo={0}&CardType={1}&CardSecurity={2}&CardHolder={3}&CardExpiry={4}&Amount={5}";

            //UrlEncode provides "escapes" for some HTTP-unfriendly characters like spaces
            //Use UrlEncode for all fields where users have freedom to input text
            //or for any field with the possibility of including HTTP-unfriendly characters
            //string cardOwner = WebUtility.HtmlEncode(checkout.CardOwner);

            data = String.Format(data, checkout.CardNumber, checkout.CardType, checkout.CSC, checkout.CardOwner, expiryDatePlaceHolder, grandTotal);

            //GET code is here as a comment only for testing purposes with BankFiction code 
            //string responseJson = w.DownloadString(webAddress + "?" + data);

            //POST - another difference from GET is method UploadString
            string responseJson = w.UploadString(webAddress, data);

            //To work with JSON we add a "using" statement at the top of this document -- using Newtonsoft.Json;
            ReservationViewModel reservation
                = JsonConvert.DeserializeObject<ReservationViewModel>(responseJson);
            if (reservation.IsReserved)
            {
                statusId = 3;
            }

            string DeliveryAddress = checkout.AddressStreet + " " + checkout.Location
            + " " + checkout.Country + " " + checkout.PostCode;

            //Now to record the transactionId and other checkout data in table "Order"
            //We need to remember the orderId - see above where we "persisted" (remembered) it in "Session"
            int orderId = Convert.ToInt32(HttpContext.Session.GetInt32("OrderId"));

            try
            {
                //Change of statusId effectively clears the cart so the Customer cannot accidently buy these goods a second time!
                //Code goes here for UPDATE SQL statement and run this SQL Command with _context.Database.ExecuteSqlCommand
                SQL = "UPDATE [Order] SET TransactionId = @p0, OrderStatusId = @p1 "
                    + ", Notes = @p2, CustomerName = @p3, DeliveryAddress = @p4 "
                    + ", Phone = @p5, EmailAddress = @p6, CardOwner = @p7 "
                    + ", CardType = @p8 "
                    + "WHERE OrderId = @p9";
                rowsChanged = _context.Database.ExecuteSqlCommand(SQL
                , reservation.TransactionId, statusId, reservation.Notes, checkout.CustomerName
                , DeliveryAddress, " ", " ", checkout.CardOwner, checkout.CardType
                , orderId);
                if (rowsChanged == 1 && statusId == 3) isSuccess = true;
            }
            catch
            {
                //Throwing an error into here is an unlikely but very serious situation.
                //It means that we have just failed to write this Order into our database but we must accept and process it
                //because we may have contracted with the Bank of Fiction to go through with it.
                //Probably best to send emails and alerts to the system admin with allowing to continue to give the standard success message.

            }

            if (reservation.IsReserved)
            {
                //Clear the Cart because we really do not want the user accidently buying their Cart-load again
                HttpContext.Session.SetInt32("OrderId", 0);

                //Changing pages so ViewBag will not work, we need the wider-scope Session to do "persistence"
                HttpContext.Session.SetString("Message", "Payment of " + grandTotal.ToString("0.00") + " is accepted. Your transaction id is " + reservation.TransactionId.ToString());
                //Success means we move on to a different web page
                return RedirectToAction("CheckoutResult");
            }
            else
            {
                ViewBag.Message = "ERROR: Could not process this credit card.";
                return View(checkout);
            }

        }

        public IActionResult CheckoutResult()
        {
            ViewBag.Message = HttpContext.Session.GetString("Message");
            return View();
        }

        public string AjaxAddToCart()
        {
            string SQLGetOrder = "";
            string SQLStartOrder = "";
            string SQLCart = "";
            string SQLBuy = "";
            string SQLUnitCostLookup = "";
            int rowsChanged = 0;
            string json = "";
            IDictionary<string, string> result = new Dictionary<string, string>();
            result["error"] = "0";


            string ProductId = Request.Query["ProductId"];
            // Security checking
            if (ProductId != null && (ProductId.Length > 20 || ProductId.IndexOf("'") > -1 || ProductId.IndexOf("#") > -1))
            {
                result["error"] = "1";
                result["error_msg"] = "Product ID is wrong";
                json = JsonConvert.SerializeObject(result);
                return json;
            }

            SQLGetOrder = "SELECT * FROM [Order] WHERE SessionId = @p0 AND OrderStatusId <= 1;";

            var orders = _context.Order.FromSql(SQLGetOrder, HttpContext.Session.Id).ToList();
            for (int iForLoop = 0; iForLoop <= 1; iForLoop++)
            {
                //Do we have an order?
                if (orders.Count == 1)
                {
                    //we have an order, we can continue to the next step
                    break;
                }
                else if (iForLoop == 1)
                {
                    //failed on the second attempt
                    result["error"] = "1";
                    result["error_msg"] = "ERROR with database table 'Order'.  This session fails to write a new order.";
                }
                else if (orders.Count > 1)
                {
                    //This would be a major error. In theory impossible but we need to be ready for any outcome
                    result["error"] = "1";
                    result["error_msg"] = "ERROR with database table 'Order'.  This session is running more than one shopping cart.";
                }
                else
                {
                    //else we get an order started
                    //150807 JPC Security improvement implementation of @p0
                    SQLStartOrder = "INSERT INTO [Order](SessionId, OrderStatusId) VALUES(@p0, 0);";
                    rowsChanged = _context.Database.ExecuteSqlCommand(SQLStartOrder, HttpContext.Session.Id);
                    // a good result would be one row changed
                    if (rowsChanged != 1)
                    {
                        //Error handling code to go in here.  Poss return a view with error messages.
                        //Code from our old webforms version is -- 
                        result["error"] = "1";
                        result["error_msg"] = "ERROR with database table 'Order'.";
                    }
                    //ready to try reading that order again
                    //150807 JPC Security improvement implementation of @p0, parameter Session.SessionID
                    orders = _context.Order.FromSql(SQLGetOrder, HttpContext.Session.Id).ToList();
                    //go round and test orders again
                }
                if (result["error"] == "1")
                {
                    json = JsonConvert.SerializeObject(result);
                    return json;
                }
            }

            //What is the OrderId
            int orderId = orders[0].OrderId;

            SQLCart = "SELECT OrderDetail.OrderId AS OrderId, OrderDetail.LineNumber As LineNumber, OrderDetail.ProductId As ProductId, " +
                "Product.Name As ProductName, Product.ImageFileName As ImageFileName, " +
                "OrderDetail.Quantity As Quantity, OrderDetail.UnitCost As UnitCost " +
                "FROM OrderDetail INNER JOIN Product ON Product.ProductId = OrderDetail.ProductId " +
                "WHERE OrderDetail.OrderId = @p0 ORDER BY OrderDetail.LineNumber;";
            var cart = _context.OrderDetailsQueryForCart.FromSql(SQLCart, orderId).ToList();

            //140903 JPC
            //What to do about a product for the case where the user clicked add to cart ..
            //IF the product is already in the cart THEN raise the quantity by one ELSE add it in

            int lineNumber = 0;
            int? quantity = 0;
            //140903 JPC cover case of user is only looking at the cart without adding any changes
            if (ProductId == null)
            {
                //use lineNumber = -1 as a flag to skip the data writing in the following "if" block
                lineNumber = -1;
            }
            else
            {
                foreach (var item in cart)
                {
                    if (item.ProductId == ProductId)
                    {
                        lineNumber = item.LineNumber;
                        quantity = item.Quantity;
                        break;
                    }
                }
            } //end if

            rowsChanged = 0;
            if (lineNumber > 0)
            {
                quantity += 1;
                //150807 JPC Security improvement implementation of @p0, @p1, @p2 - (was {0}, {1}, {2})
                SQLBuy = "UPDATE OrderDetail SET Quantity = @p0 WHERE OrderId = @p1 AND LineNumber = @p2 ";
                rowsChanged = _context.Database.ExecuteSqlCommand(SQLBuy, quantity, orderId, lineNumber);
            }
            else if (lineNumber == 0)
            {
                SQLUnitCostLookup = "SELECT * FROM Product WHERE ProductId = @p0";
                var products = _context.Product.FromSql(SQLUnitCostLookup, ProductId).ToList();
                decimal unitCost = Convert.ToDecimal(products[0].UnitCost);

                lineNumber = cart.Count + 1;
                //150807 JPC Security improvement implementation of @p0 etc
                SQLBuy = "INSERT INTO OrderDetail VALUES(@p0, @p1, @p2, @p3, @p4)";
                rowsChanged = _context.Database.ExecuteSqlCommand(SQLBuy, orderId, lineNumber, ProductId, 1, unitCost);
            }

            //If User has selected a product to add then Query is UPDATE or INSERT but they both run like this
            if (SQLBuy != "")
            {
                if (rowsChanged != 1)
                {
                    //Error handling code to go in here.  Poss return a view with error messages.
                    //Code from our old webforms version is -- 
                    result["error"] = "1";
                    result["error_msg"] = "ERROR with database table 'Order'.";
                    json = JsonConvert.SerializeObject(result);
                    return json;
                }
            }

            //Give that Session object some work to do to wake it up and get it functional
            HttpContext.Session.SetInt32("OrderId", orderId);
            //20180312 JPC use ViewBag to get the orderId to the cart for display
            ViewBag.OrderId = orderId;

            string SQLNum = "SELECT * FROM OrderDetail WHERE OrderId = @p0";
            var Num = _context.OrderDetail.FromSql(SQLNum, orderId).ToList();
            int totalNum = 0;
            foreach (var item in Num)
            {
                if (item.Quantity != null)
                {
                    totalNum += item.Quantity.Value;
                } 
            }
            result["totalNum"] = totalNum.ToString();
            json = JsonConvert.SerializeObject(result);
            return json;

        }

        //Need a C# equivalent of the IsNumeric method in Visual Basic
        //https://social.msdn.microsoft.com/Forums/windows/en-US/9b775314-098c-45f7-826b-69c904259421/is-there-a-c-equivalent-of-the-isnumeric-function-of-vb?forum=winforms
        private bool IsNumeric(string value)
        {
            bool b = true;
            int result = 0;
            int.TryParse(value, out result);
            if (result == 0) b = false;
            return b;
        }


    }
}
