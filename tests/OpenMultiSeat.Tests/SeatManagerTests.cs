using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.Tests;

[TestClass]
public class SeatManagerTests
{
    [TestMethod]
    public async Task AssignDeviceToSeatAsync_DeviceAlreadyAssignedOnDisk_FreshManagerInstanceStillRejectsReassignment()
    {
        // Regression test: SeatManager's in-memory _deviceToSeatMap must be rebuilt from
        // persisted seat data whenever seats are loaded, not just from calls made within the
        // same instance's lifetime — otherwise a brand-new SeatManager (e.g. one instantiated
        // per GUI page load, as OpenMultiSeat.GUI does) starts with an empty map and silently
        // lets an already-assigned device get assigned to a second seat.
        var seatPersistence = new FakeSeatPersistence();
        var devicePersistence = new FakeDevicePersistence();

        // Simulate a device already assigned to "seat-a" by an *earlier* SeatManager instance —
        // only the persisted Seat record reflects this, nothing in this test's memory does.
        await seatPersistence.SaveSeatAsync(new Seat
        {
            Id = "seat-a",
            Name = "Seat A",
            KeyboardIds = ["device-1"]
        });
        await seatPersistence.SaveSeatAsync(new Seat { Id = "seat-b", Name = "Seat B" });
        await devicePersistence.SaveDeviceAsync(new DeviceRecord { StableId = "device-1", HardwareId = "hw-1" });

        var manager = new SeatManager(NullLogger<SeatManager>.Instance, seatPersistence, devicePersistence);

        // Loading seats is exactly what DevicesPage does before offering the assign dialog.
        await manager.GetAllSeatsAsync();

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => manager.AssignDeviceToSeatAsync("seat-b", "device-1", InputDeviceType.Keyboard));
    }

    [TestMethod]
    public async Task AssignDeviceToSeatAsync_SameSeatAlreadyOwnsDevice_IsANoOpNotAnError()
    {
        var seatPersistence = new FakeSeatPersistence();
        var devicePersistence = new FakeDevicePersistence();

        await seatPersistence.SaveSeatAsync(new Seat { Id = "seat-a", Name = "Seat A", KeyboardIds = ["device-1"] });
        await devicePersistence.SaveDeviceAsync(new DeviceRecord { StableId = "device-1", HardwareId = "hw-1" });

        var manager = new SeatManager(NullLogger<SeatManager>.Instance, seatPersistence, devicePersistence);
        await manager.GetAllSeatsAsync();

        // Re-assigning to the seat that already owns it should not throw.
        await manager.AssignDeviceToSeatAsync("seat-a", "device-1", InputDeviceType.Keyboard);
    }

    [TestMethod]
    public async Task AssignDisplayToSeatAsync_DisplayAlreadyAssignedOnDisk_FreshManagerInstanceStillRejectsReassignment()
    {
        // Same regression as the device-assignment test above, for _displayToSeatMap.
        var seatPersistence = new FakeSeatPersistence();
        var devicePersistence = new FakeDevicePersistence();

        await seatPersistence.SaveSeatAsync(new Seat { Id = "seat-a", Name = "Seat A", DisplayIds = ["display-1"] });
        await seatPersistence.SaveSeatAsync(new Seat { Id = "seat-b", Name = "Seat B" });

        var manager = new SeatManager(NullLogger<SeatManager>.Instance, seatPersistence, devicePersistence);
        await manager.GetAllSeatsAsync();

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => manager.AssignDisplayToSeatAsync("seat-b", "display-1"));
    }

    [TestMethod]
    public async Task AssignDisplayToSeatAsync_SameSeatAlreadyOwnsDisplay_IsANoOpNotAnError()
    {
        var seatPersistence = new FakeSeatPersistence();
        var devicePersistence = new FakeDevicePersistence();

        await seatPersistence.SaveSeatAsync(new Seat { Id = "seat-a", Name = "Seat A", DisplayIds = ["display-1"] });

        var manager = new SeatManager(NullLogger<SeatManager>.Instance, seatPersistence, devicePersistence);
        await manager.GetAllSeatsAsync();

        await manager.AssignDisplayToSeatAsync("seat-a", "display-1");
    }

    [TestMethod]
    public async Task UnassignDisplayFromSeatAsync_RemovesFromDisplayIds()
    {
        var seatPersistence = new FakeSeatPersistence();
        var devicePersistence = new FakeDevicePersistence();

        await seatPersistence.SaveSeatAsync(new Seat { Id = "seat-a", Name = "Seat A", DisplayIds = ["display-1"] });

        var manager = new SeatManager(NullLogger<SeatManager>.Instance, seatPersistence, devicePersistence);
        await manager.GetAllSeatsAsync();

        await manager.UnassignDisplayFromSeatAsync("display-1");

        var seat = await manager.GetSeatAsync("seat-a");
        Assert.IsFalse(seat!.DisplayIds.Contains("display-1"));

        // Should now be assignable to a different seat without throwing.
        await seatPersistence.SaveSeatAsync(new Seat { Id = "seat-b", Name = "Seat B" });
        await manager.AssignDisplayToSeatAsync("seat-b", "display-1");
    }

    [TestMethod]
    public async Task DeleteSeatAsync_FreesItsAssignedDisplayForReassignment()
    {
        var seatPersistence = new FakeSeatPersistence();
        var devicePersistence = new FakeDevicePersistence();

        await seatPersistence.SaveSeatAsync(new Seat { Id = "seat-a", Name = "Seat A", DisplayIds = ["display-1"] });
        await seatPersistence.SaveSeatAsync(new Seat { Id = "seat-b", Name = "Seat B" });

        var manager = new SeatManager(NullLogger<SeatManager>.Instance, seatPersistence, devicePersistence);
        await manager.GetAllSeatsAsync();

        await manager.DeleteSeatAsync("seat-a");

        // display-1 should no longer be considered assigned once its owning seat is gone.
        await manager.AssignDisplayToSeatAsync("seat-b", "display-1");
    }

    private sealed class FakeSeatPersistence : ISeatPersistence
    {
        private readonly Dictionary<string, Seat> _seats = [];

        public Task<Seat?> GetSeatAsync(string seatId) => Task.FromResult(_seats.GetValueOrDefault(seatId));
        public Task<IReadOnlyList<Seat>> GetAllSeatsAsync() => Task.FromResult<IReadOnlyList<Seat>>(_seats.Values.ToList());
        public Task SaveSeatAsync(Seat seat) { _seats[seat.Id] = seat; return Task.CompletedTask; }
        public Task DeleteSeatAsync(string seatId) { _seats.Remove(seatId); return Task.CompletedTask; }
    }

    private sealed class FakeDevicePersistence : IDevicePersistence
    {
        private readonly Dictionary<string, DeviceRecord> _devices = [];

        public Task<DeviceRecord?> GetDeviceByHardwareIdAsync(string hardwareId) =>
            Task.FromResult(_devices.Values.FirstOrDefault(d => d.HardwareId == hardwareId));
        public Task<DeviceRecord?> GetDeviceByStableIdAsync(string stableId) => Task.FromResult(_devices.GetValueOrDefault(stableId));
        public Task SaveDeviceAsync(DeviceRecord device) { _devices[device.StableId] = device; return Task.CompletedTask; }
        public Task<IReadOnlyList<DeviceRecord>> GetAllDevicesAsync() => Task.FromResult<IReadOnlyList<DeviceRecord>>(_devices.Values.ToList());
        public Task DeleteDeviceAsync(string stableId) { _devices.Remove(stableId); return Task.CompletedTask; }
        public Task<string> GenerateStableIdAsync(string hardwareId, string? serialNumber = null) => Task.FromResult(hardwareId);
    }
}
