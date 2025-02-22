using HotelRoomReservationSystem.DAL;
using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.DAL.Interfaces;
using System.Globalization;
using System.Threading.RateLimiting;
using HotelRoomReservationSystem.Models.ViewModels;

namespace HotelRoomReservationSystem.BLL
{
    public class RoomTypeService : IRoomTypeService
    {
        private readonly IRoomTypeRepository roomTypeRepository;
        public RoomTypeService(IRoomTypeRepository roomTypeRepository)
        {
            this.roomTypeRepository = roomTypeRepository;
        }

        public List<RoomType> GetAllRoomType()
        {
            return roomTypeRepository.GetAllDataList();
        }
        public List<string> GetRoomTypeNameList()
        {
            return roomTypeRepository.GetRoomTypeNames();
        }

        public RoomType GetRoomTypeInfoByName(string name)
        {
            return roomTypeRepository.IsSameName(name);
        }

        public RoomType GetRoomTypeById(string Id)
        {
            return roomTypeRepository.GetRoomTypeById(Id);
        }

        public List<RoomType> GetAllRoomTypeById(string Id)
        {
            return roomTypeRepository.GetAllRoomTypeById(Id);
        }

        public bool CheckNameAvailability(string name)
        {
            return roomTypeRepository.CheckName(name);
        }

        public bool AddRoomType(RoomType roomType)
        {
            int i = roomTypeRepository.SaveRoomType(roomType);
            if (i > 0)
                return true;
            return false;
        }

        //public string GenerateId()
        //{
        //    string latestId = roomTypeRepository.GenerateId();

        //    // If no data exists, return the first ID
        //    if (string.IsNullOrEmpty(latestId))
        //        return "C001";

        //    // Ensure the ID starts with "C" and has a valid numeric part
        //    if (latestId.StartsWith("C") && int.TryParse(latestId.Substring(1), out int latestNumber))
        //    {
        //        int nextNumber = latestNumber + 1;
        //        return $"C{nextNumber.ToString("D3", CultureInfo.InvariantCulture)}"; // Format as C###
        //    }
        //    return null;
        //    //throw new InvalidOperationException("Invalid ID format in database.");
        //}

        public string GenerateCode()
        {
            string max = roomTypeRepository.getLast() ?? "RT000";

            // Validate format and extract numeric part
            if (max.StartsWith("RT") && int.TryParse(max[2..], out int n))
            {
                // Increment and generate the new code
                return (n + 1).ToString("'RT'000");
            }

            // Handle unexpected format
            throw new InvalidOperationException($"Invalid code format: {max}");
        }

        public bool IsSameName(string roomTypeName)
        {
            var allCtg = GetAllRoomType();
            foreach (var ctg in allCtg)
            {
                if (string.Equals(ctg.Name, roomTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsSameName(string roomTypeName, string roomTypeId)
        {
            try
            {
                var specCtg = GetRoomTypeById(roomTypeId);
                if (specCtg != null)
                {
                    if (string.Equals(specCtg.Name, roomTypeName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return false;
        }

        //public void UpdateCtgDetials(roomType roomTypeDetails)
        //{
        //    roomTypeRepository.UpdateDetails(roomTypeDetails);
        //}

        public bool IsSameId(string roomTypeId)
        {
            try
            {
                var roomTypes = GetAllRoomType();
                foreach (var rt in roomTypes)
                {
                    if (string.Equals(rt.Id, roomTypeId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool UpdateRooomType(RoomType roomType)
        {
            if (roomType == null || roomType.Id == null)
                return false;

            // Delegate the actual update to the repository
            return roomTypeRepository.Update(roomType);
        }
    }
}
