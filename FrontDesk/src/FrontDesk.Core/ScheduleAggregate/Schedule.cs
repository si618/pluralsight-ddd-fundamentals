using System;
using System.Collections.Generic;
using System.Linq;
using Ardalis.GuardClauses;
using FrontDesk.Core.Events;
using PluralsightDdd.SharedKernel;
using PluralsightDdd.SharedKernel.Interfaces;

namespace FrontDesk.Core.ScheduleAggregate
{
    public class Schedule : BaseEntity<Guid>, IAggregateRoot
    {
        public Schedule(Guid id,
          DateTimeOffsetRange dateRange,
          int clinicId)
        {
            Id = Guard.Against.Default(id, nameof(id));
            DateRange = dateRange;
            ClinicId = Guard.Against.NegativeOrZero(clinicId, nameof(clinicId));
        }

        private Schedule(Guid id, int clinicId) // used by EF
        {
            Id = id;
            ClinicId = clinicId;
        }

        public int ClinicId { get; private set; }
        private readonly List<Appointment> _appointments = new();
        public IEnumerable<Appointment> Appointments => _appointments.AsReadOnly();

        public DateTimeOffsetRange DateRange { get; private set; }

        public Appointment AddNewAppointment(Appointment appointment)
        {
            Guard.Against.Null(appointment, nameof(appointment));
            Guard.Against.Default(appointment.Id, nameof(appointment.Id));
            Guard.Against.DuplicateAppointment(_appointments, appointment, nameof(appointment));

            _appointments.Add(appointment);

            MarkConflictingAppointments();

            var appointmentScheduledEvent = new AppointmentScheduledEvent(appointment);
            Events.Add(appointmentScheduledEvent);

            return appointment;
        }

        public void DeleteAppointment(Appointment appointment)
        {
            Guard.Against.Null(appointment, nameof(appointment));
            var appointmentToDelete = _appointments
                                      .Where(a => a.Id == appointment.Id)
                                      .FirstOrDefault();

            if (appointmentToDelete != null)
            {
                _appointments.Remove(appointmentToDelete);
            }

            MarkConflictingAppointments();

            // TODO: Add appointment deleted event and show delete message in Blazor client app
        }




        private void MarkConflictingAppointments()
        {
            foreach (var appointment in _appointments)
            {
                var potentiallyConflictingAppointments = _appointments
                    .Where(a =>
                    {
                        if (a.Id == appointment.Id)
                        {
                            return false;
                        }

                        var overlaps = a.TimeRange.Overlaps(appointment.TimeRange);
                        if (!overlaps)
                        {
                            return false;
                        }

                        // Patients, rooms and doctors should not have two overlapping appointments
                        var isPotentiallyConflicting =
                            a.PatientId == appointment.PatientId ||
                            a.RoomId == appointment.RoomId ||
                            a.DoctorId == appointment.DoctorId;
                        a.IsPotentiallyConflicting = isPotentiallyConflicting;
                        return isPotentiallyConflicting;
                    });

                appointment.IsPotentiallyConflicting = potentiallyConflictingAppointments.Any();
            }
        }

        /// <summary>
        /// Call any time this schedule's appointments are updated directly
        /// </summary>
        public void AppointmentUpdatedHandler()
        {
            // TODO: Add ScheduleHandler calls to UpdateDoctor, UpdateRoom to complete additional rules described in MarkConflictingAppointments
            MarkConflictingAppointments();
        }
    }
}
