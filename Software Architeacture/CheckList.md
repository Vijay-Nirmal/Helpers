## Check list to gather information

- [ ] Ask for every requirement
  - [ ] Ask questions that user mignt not think off or say
- [ ] Ask for use cases
  - [ ] Identify all actor/users
  - [ ] Capture and describe all possible user cases
- [ ] Ask for user flow
  - [ ] Expand each use case through flow of events (Events contains "actions" and "data")
  - [ ] Create Sequence Diagram if Needed
- [ ] Call out unrealistic expectation/requirement

## Constraints analysis

- [ ] Technical Constraints
- [ ] Business Contraints
- [ ] Legal Constraints

## Think about Quality Attributes

- [ ] Performance
  - [ ] Response Time (Processing Time + Waiting Time (Queue Time, Network delay, others))
  - [ ] Throughput
- [ ] Scalablity
  - [ ] Vertical Scaling
  - [ ] Horizontal Scaling
  - [ ] Team Scalablity (Allowing multiple teams to work in parallel with minimal dependency)
- [ ] Availability
  - [ ] MTBF - Mean Time Between Failures
  - [ ] MTTR - Mean Time to Recovery
  - [ ] Availability = MTBF / (MTBF + MTTR)
  - [ ] Fault Tolerance
    - [ ] Redundancy and Replication - Spatial Redundancy
    - [ ] Retryies and Redelivery - Time Redundancy
  - [ ] Failure Detection (Health Check)
- [ ] Testability
- [ ] Deployability
- [ ] Maintainability
- [ ] Portability
- [ ] Security
- [ ] Observability
- [ ] Consistency
- [ ] Efficiency
- [ ] Usability

## Important agreement or promises to the users

- [ ] SLA - Service Level Agreement
  - The penalties and financial consequences if we breach the contract
- [ ] SLOs - Service Level Objectives
  - Individual goals set for our system
  - Target value/range that our service needs to meet
- [ ] SLIs - Service Level Indicators
  - Quantitative measure of our compliance with a service level objectives (SLOs)

## Measurement
- [ ] Analyze the response time using percentile
- [ ] Find perforamce degration point when load increaces