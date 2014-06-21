package org.unbiquitous.unity.androidnetwork;

public class UdpReceiveData {
	private byte[] data;
	private String address;
	private int port;
	
	public UdpReceiveData(byte[] data, String address, int port) {
		this.data = data;
		this.address = address;
		this.port = port;
	}
	
	public byte[] getData() {
		return data;
	}
	
	public String getAddress() {
		return address;
	}
	
	public int getPort(){
		return port;
	}
}
