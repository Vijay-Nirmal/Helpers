## System Design Flow

1. Requirements Engineering
2. Capacity Estimation
  - [ ] Excepted throughput calculation (Average and Peek load)
  - [ ] Excepted bandwidth calculation (Throughput * (Request + Response Size))
  - [ ] Excepted storage requirement calculation over a long duration (5 years)
3. Data Model
4. API Design
5. System Design
  - [ ] Find and desing based on Read Heavy or Write Heavy system
6. POC with the design

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

- [ ] Note: Quality Attributes will/can be different for each user flow or a single action
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
  - [ ] Make the trade-off between Availability and Consistancy (CAP Theorem)
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

## Rest API Definition

- [ ] Sequence Diagram will be very usefull to start
- [ ] Identifying Entites
- [ ] Mapping Entities to URIs
- [ ] Defining Resource's Representation (Structure of Object)
- [ ] Assigning HTTP Methods to Operations on Resources
- [ ] API feature like Auth and Paging

## Design Architectural Diagram
- [ ] Design functional Architectural Diagram
- [ ] Design non-functional Architectural Diagram (Scalability, Avaliablity ....)
  - [ ] Scalability: Add Shard (Spreading data accross multiple instance (Compound Index + Range Shard Strategy))
  - [ ] Performance: CDN (Find update model), Cache reponse, Add Index
- [ ] Fault Tolerance: Replication, Geo Replication
- [ ] Availability
- [ ] Durability: Backup