using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NWRestfulAPI.Models;

namespace NWRestfulAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {

        // Alustetaan tietokantayhteys
        NorthwindOriginalContext db = new NorthwindOriginalContext();


        // Hakee kaikki asiakkaat
        [HttpGet]
        public ActionResult GetAllCustomers()
        {

            Console.WriteLine("Haetaan kaikki asiakkaat");

            var customers = db.Customers.ToList();
            return Ok(customers);
        }


        // Hakee asiakkaan ID:n perusteella
        [HttpGet]
        [Route("{id}")]
        public ActionResult GetCustomers(string id)
        {
            var customer = db.Customers.Find(id);
            if (customer == null)
            {
                return NoContent();
            }
            else
            {
                return Ok(customer.Orders.ToList());
            }
        }


        // Muokkaa asiakkaan tietoja
        [HttpPut]
        [Route("{id}")]
        public ActionResult EditCustomer(string id, [FromBody] Customer cust)
        {

            var custToEdit = db.Customers.Find(id);
            if (custToEdit == null)
            {
                return NotFound("Asiakasta ei löydy tietokannasta");
            }

            // MUOKKAUS – kopioi kaikki kentät ilman luettelua
            db.Entry(custToEdit).CurrentValues.SetValues(cust);

            db.SaveChanges();
            return Ok("Muokattiin: " + custToEdit.CompanyName);
        }


        // Lisää uuden asiakkaan
        [HttpPost]
        public ActionResult AddCustomer([FromBody] Customer cust)
        {
            try
            {
                var existingCustomer = db.Customers.Where(c => c.CompanyName == cust.CompanyName);
                if (existingCustomer.Count() > 0)
                {
                    return BadRequest("Asiakas on jo olemassa: " + cust.CompanyName);
                }
                db.Customers.Add(cust);
                db.SaveChanges();
                return Ok("Added new customer " + cust.CompanyName);
            }
            catch (Exception ex)
            {
                return BadRequest("Tapahtui virhe. Lue lisää: " + ex.InnerException);
            }
        }


        // Hakee asiakkaat nimen osalla
        [HttpGet]
        [Route("company/{search}")]
        public ActionResult GetCustomersByName(string search)
        {
            var customers = db.Customers.Where(c => c.CompanyName.Contains(search)).ToList(); // <--- nimen osalla haku
            //var customers = db.Customers.Where(c => c.CompanyName.StartsWith(search)).ToList(); <--- nimen alulla haku

            //var customers = db.Customers.Where(c => c.CompanyName == search).ToList(); <--- täydellinen match

            if (customers.Count == 0)
            {
                return NoContent();
            }
            else
            {
                return Ok(customers);
            }
        }
        // Poistaa asiakkaan ID:n perusteella
        [HttpDelete]
        [Route("{id}")]
        public ActionResult DeleteCustomer(string id)
        {
            try
            {
                var customer = db.Customers.Find(id);
                if (customer == null)
                {
                    return NoContent();
                }

                db.Customers.Remove(customer);
                db.SaveChanges();
                return Ok("Poistettu asiakas: " + customer.CompanyName);
            }
            catch (Exception ex)
            {
                return BadRequest("Virhe poistettaessa asiakasta. Lue lisää: " + ex.Message);
            }
        }


    }
}
