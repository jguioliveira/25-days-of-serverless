import React, { Component } from 'react'
import axios from 'axios'
import * as signalR from '@aspnet/signalr'


class ServiceList extends Component {

    constructor(props) {
        super(props)

        this.state = {
            services: [],
            errorMessage: null,
            hubConnection: null
        }

        let teste = [];
    }

    componentDidMount() {

        axios.get('https://25-days-of-serverless-challenge-8.azurewebsites.net/api/services')
            .then(response => {
                this.setState({
                    services: response.data
                })
                console.log(response)
            })
            .catch(error => {
                this.setState({
                    errorMessage: error
                })
                console.log(error)
            });

        const hubConnection = new signalR.HubConnectionBuilder()
            .withUrl('https://25-days-of-serverless-challenge-8.azurewebsites.net/api')
            .build();

        this.setState({ hubConnection });

        this.setState((prevState) => {
            prevState.hubConnection
                .start()
                .then(() => console.log('Connection started!'))
                .catch(err => console.log('Error while establishing connection :('));

            prevState.hubConnection.on('updated', updatedServices => {
                let services = this.state.services;

                if (Array.isArray(updatedServices)) {
                    updatedServices.forEach(updatedService => {
                        let service = services.find(s => s.id === updatedService.id);
                        if (service) service.status = updatedService.Status;
                    })
                }
                else {
                    let service = services.find(s => s.id === updatedServices.id);
                    if (service) service.status = updatedServices.Status;
                }

                this.setState({ services });
            });
        });
    }


    render() {
        const { services } = this.state
        return (
            <div>
                <h3>List of Services</h3>
                <table class="table">
                    <thead class="thead-dark">
                        <tr>
                            <th scope="col">Id</th>
                            <th scope="col">Name</th>
                            <th scope="col">Region</th>
                            <th scope="col">Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        {
                            services.length ?
                                services.map(service =>
                                    <tr key={service.id}>
                                        <td>{service.id}</td>
                                        <td>{service.name}</td>
                                        <td>{service.region}</td>
                                        <td>{service.status}</td>
                                    </tr>
                                ) :
                                null
                        }
                    </tbody>
                </table>
            </div>
        )
    }
}

export default ServiceList
