
using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ServerLibrary.Data;
using ServerLibrary.Repositories.Contracts;
using System;
using System.Transactions;

namespace ServerLibrary.Repositories.Implementations
{
    public class EmployeeRepository(AppDbContext dbContext) : IGenericRepositoryInterface<Employee>
    {
        public async Task<GeneralResponse> DeleteById(int id)
        {
            var item = await dbContext.Employees.FindAsync(id);
            if (item is null) return NotFound();

            dbContext.Employees.Remove(item);
            await Commit();
            return Success();
        }

        public async Task<List<Employee>> GetAll()
        {
            var employees = await dbContext.Employees
                .AsNoTracking()
                .Include(t => t.Town)
                .ThenInclude(b => b.City)
                .ThenInclude(c => c.Country)
                .Include(b => b.Branch)
                .ThenInclude(d => d.Department)
                .ThenInclude(gd => gd.GeneralDepartment)
                .ToListAsync();
            return employees;
        }


        public async Task<Employee> GetById(int id)
        {
            var employee = await dbContext.Employees
               .AsNoTracking()
               .Include(t => t.Town)
               .ThenInclude(b => b.City)
               .ThenInclude(c => c.Country)
               .Include(b => b.Branch)
               .ThenInclude(d => d.Department)
               .ThenInclude(gd => gd.GeneralDepartment)
               .FirstOrDefaultAsync(ei => ei.Id == id);
            return employee!;
        }

        public async Task<GeneralResponse> Insert(Employee item)
        {
            if (!await CheckName(item.Name!)) return new GeneralResponse(false, "Employee already added");

            dbContext.Employees.Add(item);
            await Commit();
            return Success();
        }


        public async Task<GeneralResponse> Update(Employee item)
        {   
            var findUser = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == item.Id);
            if (findUser is  null) return new GeneralResponse(false, "Employee does not exist");

            findUser.Name = item.Name;
            findUser.Other = item.Other;
            findUser.Address = item.Address;
            findUser.TelephoneNumber = item.TelephoneNumber;
            findUser.BranchId = item.BranchId;
            findUser.TownId = item.TownId;
            findUser.CivilId = item.CivilId;
            findUser.FileNumber = item.FileNumber;
            findUser.JobName = item.JobName;
            findUser.Photo = item.Photo;

            await dbContext.SaveChangesAsync();
            await Commit();
            return Success();
        }



        private static GeneralResponse NotFound() => new(false, "Sorry Employee not found");
        private static GeneralResponse Success() => new(true, "Process completed");
        private async Task Commit() => await dbContext.SaveChangesAsync();
        private async Task<bool> CheckName(string name)
        {
            var item = await dbContext.Employees.FirstOrDefaultAsync(x => x.Name!.ToLower().Equals(name.ToLower()));
            return item is null ? true : false;
        }
    }
}
