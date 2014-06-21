package org.unbiquitous.unity.androidnetwork;

import java.io.IOException;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.SocketException;
import java.net.UnknownHostException;

public class UdpClient {
	private DatagramSocket udpSocket;

	public UdpClient(String host, int port) throws SocketException,
			UnknownHostException {
		udpSocket = new DatagramSocket(port, InetAddress.getByName(host));
	}

	public boolean getEnableBroadcast() throws SocketException {
		return udpSocket.getBroadcast();
	}

	public void setEnableBroadcast(boolean value) throws SocketException {
		udpSocket.setBroadcast(value);
	}

	public boolean isConnected() {
		return udpSocket.isConnected();
	}

	public boolean getReuseAddress() throws SocketException {
		return udpSocket.getReuseAddress();
	}

	public void setReuseAddress(boolean value) throws SocketException {
		udpSocket.setReuseAddress(value);
	}

	public int getReceiveTimeout() throws SocketException {
		return udpSocket.getSoTimeout();
	}

	public void setReceiveTimout(int value) throws SocketException {
		udpSocket.setSoTimeout(value);
	}

	public void connect(String host, int port) throws SocketException,
			UnknownHostException {
		udpSocket.connect(InetAddress.getByName(host), port);
	}

	public int send(byte[] datagram, int bytes, String host, int port)
			throws IOException, UnknownHostException {
		DatagramPacket packet = new DatagramPacket(datagram, bytes,
				InetAddress.getByName(host), port);
		udpSocket.send(packet);
		return bytes;
	}

	public UdpReceiveData receive() throws IOException {
		byte[] buffer = new byte[4096];
		DatagramPacket packet = new DatagramPacket(buffer, buffer.length);
		udpSocket.receive(packet);

		buffer = new byte[packet.getLength()];
		System.arraycopy(packet.getData(), 0, buffer, 0, buffer.length);
		return new UdpReceiveData(buffer, packet.getAddress().getHostAddress(),
				packet.getPort());
	}

	public void close() {
		udpSocket.close();
	}
}
